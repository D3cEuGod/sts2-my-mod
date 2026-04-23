using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sts2DpsPrototype;

internal static class DamageHookPatches
{
    private static bool _patched;
    private static readonly List<RecentDamageRecord> RecentHistoryRecords = new();
    private static readonly List<CachedPoisonSource> CachedPoisonSources = new();
    private static readonly FieldInfo? PowerOwnerField = AccessTools.Field(typeof(PowerModel), "_owner");
    private static readonly FieldInfo? PowerApplierField = AccessTools.Field(typeof(PowerModel), "_applier");
    private static readonly FieldInfo? PowerAmountField = AccessTools.Field(typeof(PowerModel), "_amount");
    private static readonly FieldInfo? PowerAmountOnTurnStartField = AccessTools.Field(typeof(PowerModel), "_amountOnTurnStart");
    private static PendingVictoryHit? _pendingVictoryHit;
    private static CachedRecentDealer? _recentDoomDealer;

    internal static void EnsurePatched()
    {
        if (_patched)
            return;

        var harmony = new Harmony($"{MainFile.ModId}.harmony");
        harmony.Patch(
            original: AccessTools.Method(typeof(CombatHistory), nameof(CombatHistory.DamageReceived)),
            postfix: new HarmonyMethod(typeof(DamageHookPatches), nameof(OnDamageReceived)));

        harmony.Patch(
            original: AccessTools.Method(typeof(PlayCardAction), "ExecuteAction"),
            prefix: new HarmonyMethod(typeof(DamageHookPatches), nameof(OnPlayCardActionExecute)));

        harmony.Patch(
            original: AccessTools.Method(typeof(PoisonPower), nameof(PoisonPower.AfterSideTurnStart)),
            postfix: new HarmonyMethod(typeof(DamageHookPatches), nameof(OnPoisonAfterSideTurnStartObserved)));

        harmony.Patch(
            original: AccessTools.Method(typeof(DoomPower), nameof(DoomPower.DoomKill)),
            postfix: new HarmonyMethod(typeof(DamageHookPatches), nameof(OnDoomKillObserved)));

        var singleTargetDamageOverloads = typeof(CreatureCmd)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name == nameof(CreatureCmd.Damage)
                && method.ReturnType == typeof(Task<IEnumerable<DamageResult>>)
                && method.GetParameters().Length >= 2
                && method.GetParameters()[0].ParameterType == typeof(PlayerChoiceContext)
                && method.GetParameters()[1].ParameterType == typeof(Creature))
            .ToArray();

        foreach (var overload in singleTargetDamageOverloads)
        {
            harmony.Patch(
                original: overload,
                postfix: new HarmonyMethod(typeof(DamageHookPatches), nameof(OnCreatureCmdDamageSingleObserved)));
        }

        _patched = true;
        MainFile.Log.Info("Patched CombatHistory.DamageReceived for DPS tracking, PlayCardAction.ExecuteAction for final-hit context, PoisonPower/DoomPower for combat-only debuff fallback, and CreatureCmd.Damage for lethal fallback observation.");
    }

    private static void OnDamageReceived(CombatState combatState, Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
    {
        if (dealer != null && IsPoisonCard(cardSource) && receiver.IsEnemy)
            CachePoisonSource(receiver, dealer);

        Creature? resolvedDealer = dealer;
        if (resolvedDealer == null && cardSource == null)
            resolvedDealer = ResolveCachedPoisonSource(receiver, null);

        RememberHistoryHit(receiver, resolvedDealer, result, cardSource);
        RecordTrackedDamage(combatState, receiver, resolvedDealer, result, cardSource, resolvedDealer == dealer ? "COMBAT_HISTORY" : "COMBAT_HISTORY_CACHED_POISON");
    }

    private static void OnPlayCardActionExecute(PlayCardAction __instance)
    {
        Creature? target = __instance.Target;
        Player? player = __instance.Player;
        Creature? dealer = player?.Creature;
        if (target == null || dealer == null || player == null)
            return;

        if (!target.IsEnemy)
            return;

        CardModel? card = AccessTools.Field(typeof(PlayCardAction), "_card")?.GetValue(__instance) as CardModel;
        MaybeCachePoisonSourceFromCard(target, dealer, card);
        MaybeCacheRecentDoomDealer(dealer, card);
        _pendingVictoryHit = new PendingVictoryHit(
            DateTime.UtcNow.Ticks,
            player.NetId.ToString(),
            ResolveOwnerDisplayName(null, player),
            target.Name ?? target.GetType().Name ?? "<null>",
            dealer.Name ?? dealer.GetType().Name ?? "<null>",
            card?.GetType().Name ?? "<null>",
            Math.Max(0, target.CurrentHp),
            Math.Max(0, target.Block));
    }

    private static void OnPoisonAfterSideTurnStartObserved(PoisonPower __instance, CombatSide side, CombatState combatState, ref Task __result)
    {
        Creature? receiver = ResolvePowerOwner(__instance);
        Creature? liveDealer = ResolvePowerApplier(__instance);

        if (side != CombatSide.Enemy)
            return;

        if (receiver == null)
            return;

        CachePoisonSource(receiver, liveDealer);
        Creature? dealer = ResolveCachedPoisonSource(receiver, liveDealer);
        if (dealer == null)
            return;

        Player? owner = ResolveOwningPlayer(dealer);
        if (owner == null || !receiver.IsEnemy)
            return;

        if (ResolveOwningPlayer(receiver)?.NetId == owner.NetId)
            return;

        int expectedDamage = ResolvePoisonTickDamage(__instance);
        int preHp = Math.Max(0, receiver.CurrentHp);
        int trackedDamage = Math.Min(preHp, expectedDamage);
        if (trackedDamage <= 0)
            return;

        long observationStart = DateTime.UtcNow.Ticks;
        string playerId = owner.NetId.ToString();
        string playerName = ResolveOwnerDisplayName(combatState, owner);
        __result = ObservePoisonTickAsync(__result, receiver, dealer, playerId, playerName, expectedDamage, preHp, trackedDamage, observationStart);
    }

    private static async Task ObservePoisonTickAsync(
        Task originalTask,
        Creature receiver,
        Creature dealer,
        string playerId,
        string playerName,
        int expectedDamage,
        int preHp,
        int trackedDamage,
        long observationStart)
    {
        await originalTask;

        bool hasHistory = HasRecentHistorySince(receiver, "<null>", observationStart);
        int postHp = Math.Max(0, receiver.CurrentHp);
        int actualDamage = Math.Max(0, preHp - postHp);

        if (hasHistory)
            return;

        int inferredDamage = Math.Min(trackedDamage, actualDamage);
        if (inferredDamage <= 0)
        {
            if (!receiver.IsDead)
                return;

            inferredDamage = trackedDamage;
        }

        MainFile.Log.Info($"[POISON_FALLBACK] owner={playerName} target={receiver.Name ?? receiver.GetType().Name ?? "<null>"} dealer={dealer.Name ?? dealer.GetType().Name ?? "<null>"} expected={expectedDamage} preHp={preHp} postHp={postHp} dead={receiver.IsDead}");
        RecordInferredDamage(playerId, playerName, receiver, dealer, "<null>", inferredDamage, "POISON_POWER_FALLBACK");
    }

    private static void OnDoomKillObserved(DoomPower __instance, IReadOnlyList<Creature> creatures, ref Task __result)
    {
        Creature? dealer = ResolvePowerApplier(__instance) ?? ResolveRecentDoomDealer() ?? ResolveCurrentSinglePlayerDealer();
        List<PendingDoomKill> pending = creatures
            .Where(creature => creature != null)
            .Select(creature => new PendingDoomKill(creature, Math.Max(0, creature.CurrentHp), Math.Max(0, creature.Block)))
            .ToList();
        __result = ObserveDoomKillAsync(__result, dealer, pending);
    }

    private static async Task ObserveDoomKillAsync(Task originalTask, Creature? dealer, List<PendingDoomKill> pending)
    {
        await originalTask;

        if (dealer == null || pending.Count == 0)
            return;

        Player? owner = ResolveOwningPlayer(dealer);
        if (owner == null)
            return;

        string ownerName = ResolveOwnerDisplayName(null, owner);
        foreach (var item in pending)
        {
            Creature receiver = item.Receiver;
            if (!receiver.IsEnemy)
                continue;

            if (ResolveOwningPlayer(receiver)?.NetId == owner.NetId)
                continue;

            if (!receiver.IsDead && receiver.CurrentHp > 0)
                continue;

            if (HasRecentHistoryContext(receiver, dealer, "<null>", 2))
                continue;

            int trackedDamage = item.PreHp;
            if (trackedDamage <= 0)
                continue;

            MainFile.Log.Info($"[DOOM_FALLBACK] owner={ownerName} target={receiver.Name ?? receiver.GetType().Name ?? "<null>"} dealer={dealer.Name ?? dealer.GetType().Name ?? "<null>"} preHp={item.PreHp} preBlock={item.PreBlock} postHp={receiver.CurrentHp} dead={receiver.IsDead}");
            RecordInferredDamage(owner.NetId.ToString(), ownerName, receiver, dealer, "<null>", trackedDamage, "DOOM_POWER_FALLBACK");
        }
    }

    private static void OnCreatureCmdDamageSingleObserved(
        MethodBase __originalMethod,
        object[] __args,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        Creature? target = __args.OfType<Creature>().FirstOrDefault();
        Creature? dealer = __args.OfType<Creature>().Skip(1).FirstOrDefault();
        CardModel? cardSource = __args.OfType<CardModel>().FirstOrDefault();
        __result = ObserveSingleDamageAsync(__result, __originalMethod.Name, target, dealer, cardSource);
    }

    private static async Task<IEnumerable<DamageResult>> ObserveSingleDamageAsync(
        Task<IEnumerable<DamageResult>> originalTask,
        string overloadName,
        Creature? target,
        Creature? dealer,
        CardModel? cardSource)
    {
        IEnumerable<DamageResult> results = await originalTask;
        DamageResult? matchedResult = results.FirstOrDefault(result => ReferenceEquals(result.Receiver, target)) ?? results.FirstOrDefault();
        Creature? receiver = matchedResult?.Receiver ?? target;
        if (matchedResult != null && receiver != null && dealer != null && matchedResult.WasTargetKilled && ShouldApplyLethalFallback(receiver, dealer, matchedResult, cardSource))
        {
            MainFile.Log.Info($"[DAMAGE_LETHAL_FALLBACK] overload={overloadName} target={receiver.Name ?? receiver.GetType().Name ?? "<null>"} dealer={dealer.Name ?? dealer.GetType().Name ?? "<null>"} card={cardSource?.GetType().Name ?? "<null>"} total={matchedResult.TotalDamage} unblocked={matchedResult.UnblockedDamage} blocked={matchedResult.BlockedDamage} overkill={matchedResult.OverkillDamage}");
            RecordTrackedDamage(null, receiver, dealer, matchedResult, cardSource, "CREATURE_CMD_FALLBACK");
        }

        return results;
    }

    private static bool ShouldApplyLethalFallback(Creature receiver, Creature dealer, DamageResult result, CardModel? cardSource)
    {
        CompactRecentHistory();

        if (ResolveOwningPlayer(dealer) == null)
            return false;

        if (ResolveOwningPlayer(receiver)?.NetId == ResolveOwningPlayer(dealer)?.NetId)
            return false;

        string cardName = cardSource?.GetType().Name ?? "<null>";
        string receiverName = receiver.Name ?? receiver.GetType().Name ?? "<null>";
        string dealerName = dealer.Name ?? dealer.GetType().Name ?? "<null>";

        return !RecentHistoryRecords.Any(record =>
            record.TimestampTicks >= DateTime.UtcNow.Ticks - TimeSpan.FromSeconds(2).Ticks
            && record.CardName == cardName
            && record.ReceiverName == receiverName
            && record.DealerName == dealerName
            && record.TotalDamage == result.TotalDamage
            && record.UnblockedDamage == result.UnblockedDamage
            && record.BlockedDamage == result.BlockedDamage
            && record.OverkillDamage == result.OverkillDamage);
    }

    private static void RememberHistoryHit(Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
    {
        CompactRecentHistory();
        RecentHistoryRecords.Add(new RecentDamageRecord(
            DateTime.UtcNow.Ticks,
            receiver.Name ?? receiver.GetType().Name ?? "<null>",
            dealer?.Name ?? dealer?.GetType().Name ?? "<null>",
            cardSource?.GetType().Name ?? "<null>",
            result.TotalDamage,
            result.UnblockedDamage,
            result.BlockedDamage,
            result.OverkillDamage));
    }

    private static void CompactRecentHistory()
    {
        long cutoff = DateTime.UtcNow.Ticks - TimeSpan.FromSeconds(4).Ticks;
        RecentHistoryRecords.RemoveAll(record => record.TimestampTicks < cutoff);
    }

    private static void CompactCachedPoisonSources()
    {
        long cutoff = DateTime.UtcNow.Ticks - TimeSpan.FromSeconds(30).Ticks;
        CachedPoisonSources.RemoveAll(record => record.TimestampTicks < cutoff);
    }

    internal static void TryApplyPendingVictoryFallback(CombatState state)
    {
        CompactRecentHistory();

        if (_pendingVictoryHit == null)
            return;

        if (_pendingVictoryHit.TimestampTicks < DateTime.UtcNow.Ticks - TimeSpan.FromSeconds(3).Ticks)
        {
            _pendingVictoryHit = null;
            return;
        }

        bool alreadyRecorded = RecentHistoryRecords.Any(record =>
            record.TimestampTicks >= _pendingVictoryHit.TimestampTicks
            && record.CardName == _pendingVictoryHit.CardName
            && record.ReceiverName == _pendingVictoryHit.TargetName
            && record.DealerName == _pendingVictoryHit.DealerName);

        if (alreadyRecorded)
        {
            _pendingVictoryHit = null;
            return;
        }

        int trackedDamage = _pendingVictoryHit.TargetHp + _pendingVictoryHit.TargetBlock;
        if (trackedDamage <= 0 || state.Enemies.Count != 0)
        {
            _pendingVictoryHit = null;
            return;
        }

        MainFile.Log.Info($"[DAMAGE_VICTORY_FALLBACK] owner={_pendingVictoryHit.PlayerName} target={_pendingVictoryHit.TargetName} dealer={_pendingVictoryHit.DealerName} card={_pendingVictoryHit.CardName} inferredTracked={trackedDamage} preHp={_pendingVictoryHit.TargetHp} preBlock={_pendingVictoryHit.TargetBlock}");
        DpsTracker.RecordDamage(_pendingVictoryHit.PlayerId, _pendingVictoryHit.PlayerName, trackedDamage);
        _pendingVictoryHit = null;
    }

    private static bool HasRecentHistoryContext(Creature receiver, Creature dealer, string cardName, int windowSeconds)
    {
        CompactRecentHistory();
        string receiverName = receiver.Name ?? receiver.GetType().Name ?? "<null>";
        string dealerName = dealer.Name ?? dealer.GetType().Name ?? "<null>";

        return RecentHistoryRecords.Any(record =>
            record.TimestampTicks >= DateTime.UtcNow.Ticks - TimeSpan.FromSeconds(windowSeconds).Ticks
            && record.CardName == cardName
            && record.ReceiverName == receiverName
            && record.DealerName == dealerName);
    }

    private static bool HasRecentHistorySince(Creature receiver, string cardName, long timestampTicks)
    {
        CompactRecentHistory();
        string receiverName = receiver.Name ?? receiver.GetType().Name ?? "<null>";

        return RecentHistoryRecords.Any(record =>
            record.TimestampTicks >= timestampTicks
            && record.CardName == cardName
            && record.ReceiverName == receiverName);
    }

    private static void RecordInferredDamage(string playerId, string playerName, Creature receiver, Creature dealer, string cardName, int trackedDamage, string sourceTag)
    {
        if (trackedDamage <= 0)
            return;

        string targetName = receiver.Name ?? receiver.GetType().Name ?? "<null>";
        string dealerName = dealer.Name ?? dealer.GetType().Name ?? "<null>";
        string petOwner = dealer.PetOwner?.NetId.ToString() ?? "<none>";
        MainFile.Log.Info($"[DAMAGE_FINAL] source={sourceTag} owner={playerName} target={targetName} dealer={dealerName} card={cardName} tracked={trackedDamage} petOwner={petOwner}");
        DpsTracker.RecordDamage(playerId, playerName, trackedDamage);
    }

    private static void RecordTrackedDamage(CombatState? combatState, Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource, string sourceTag)
    {
        Player? owner = ResolveOwningPlayer(dealer);
        if (owner == null)
            return;

        Player? receiverOwner = ResolveOwningPlayer(receiver);
        if (receiverOwner?.NetId == owner.NetId)
        {
            string selfCardName = cardSource?.GetType().Name ?? "<null>";
            string selfTargetName = receiver.Name ?? receiver.GetType().Name ?? "<null>";
            string selfDealerName = dealer?.Name ?? dealer?.GetType().Name ?? "<null>";
            MainFile.Log.Info($"[DAMAGE_SKIPPED_SELF] source={sourceTag} owner={ResolveOwnerDisplayName(combatState, owner)} target={selfTargetName} dealer={selfDealerName} card={selfCardName} total={result.TotalDamage} unblocked={result.UnblockedDamage} blocked={result.BlockedDamage} overkill={result.OverkillDamage}");
            return;
        }

        int damage = GetTrackedDamage(result);
        if (damage <= 0)
            return;

        string playerId = owner.NetId.ToString();
        string playerName = ResolveOwnerDisplayName(combatState, owner);
        string targetName = receiver.Name ?? receiver.GetType().Name ?? "<null>";
        string dealerName = dealer?.Name ?? dealer?.GetType().Name ?? "<null>";
        string cardName = cardSource?.GetType().Name ?? "<null>";
        string petOwner = dealer?.PetOwner?.NetId.ToString() ?? "<none>";
        MainFile.Log.Info($"[DAMAGE_FINAL] source={sourceTag} owner={playerName} target={targetName} dealer={dealerName} card={cardName} total={result.TotalDamage} unblocked={result.UnblockedDamage} blocked={result.BlockedDamage} overkill={result.OverkillDamage} tracked={damage} killed={result.WasTargetKilled} petOwner={petOwner}");
        DpsTracker.RecordDamage(playerId, playerName, damage);
    }

    private static Creature? ResolvePowerOwner(PowerModel power)
    {
        return PowerOwnerField?.GetValue(power) as Creature ?? power.Target;
    }

    private static Creature? ResolvePowerApplier(PowerModel? power)
    {
        if (power == null || PowerApplierField == null)
            return null;

        try
        {
            return PowerApplierField.GetValue(power) as Creature;
        }
        catch (TargetException)
        {
            return null;
        }
    }

    private static void CachePoisonSource(Creature receiver, Creature? dealer)
    {
        if (dealer == null)
            return;

        CompactCachedPoisonSources();
        CachedPoisonSources.RemoveAll(record => ReferenceEquals(record.Receiver, receiver));
        CachedPoisonSources.Add(new CachedPoisonSource(DateTime.UtcNow.Ticks, receiver, dealer));
    }

    private static void MaybeCachePoisonSourceFromCard(Creature receiver, Creature dealer, CardModel? card)
    {
        if (!IsPoisonCard(card))
            return;

        CachePoisonSource(receiver, dealer);
    }

    private static void MaybeCacheRecentDoomDealer(Creature dealer, CardModel? card)
    {
        if (!IsDoomCard(card))
            return;

        _recentDoomDealer = new CachedRecentDealer(DateTime.UtcNow.Ticks, dealer);
    }

    private static Creature? ResolveRecentDoomDealer()
    {
        if (_recentDoomDealer == null)
            return null;

        if (_recentDoomDealer.TimestampTicks < DateTime.UtcNow.Ticks - TimeSpan.FromMinutes(2).Ticks)
        {
            _recentDoomDealer = null;
            return null;
        }

        return _recentDoomDealer.Dealer;
    }

    private static Creature? ResolveCurrentSinglePlayerDealer()
    {
        CombatState? state = CombatManager.Instance?.DebugOnlyGetState();
        if (state == null || state.Players.Count != 1)
            return null;

        return state.Players[0].Creature;
    }

    private static bool IsPoisonCard(CardModel? card)
    {
        string cardName = card?.GetType().Name ?? string.Empty;
        return cardName.Contains("Poison", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDoomCard(CardModel? card)
    {
        string cardName = card?.GetType().Name ?? string.Empty;
        return cardName.Contains("Doom", StringComparison.OrdinalIgnoreCase)
            || cardName.Contains("BorrowedTime", StringComparison.OrdinalIgnoreCase);
    }

    private static Creature? ResolveCachedPoisonSource(Creature receiver, Creature? dealer)
    {
        if (dealer != null)
            return dealer;

        CompactCachedPoisonSources();
        return CachedPoisonSources.LastOrDefault(record => ReferenceEquals(record.Receiver, receiver))?.Dealer;
    }

    private static int ResolvePoisonTickDamage(PoisonPower power)
    {
        object? amountOnTurnStart = PowerAmountOnTurnStartField?.GetValue(power);
        if (amountOnTurnStart is int startAmount && startAmount > 0)
            return startAmount;

        object? amount = PowerAmountField?.GetValue(power);
        if (amount is int currentAmount && currentAmount > 0)
            return currentAmount;

        return 0;
    }

    private static Player? ResolveOwningPlayer(Creature? dealer)
    {
        if (dealer == null)
            return null;

        if (dealer.Player != null)
            return dealer.Player;

        if (dealer.PetOwner != null)
            return dealer.PetOwner;

        return null;
    }

    private static string ResolveOwnerDisplayName(CombatState? combatState, Player owner)
    {
        if (combatState != null)
            return CombatRuntimeBridge.ResolveDisplayName(combatState, owner);

        if (!string.IsNullOrWhiteSpace(owner.Creature?.Name))
            return owner.Creature.Name;

        string? characterName = owner.Character?.Title?.ToString();
        if (!string.IsNullOrWhiteSpace(characterName))
            return characterName;

        return $"Player {owner.NetId}";
    }

    private static int GetTrackedDamage(DamageResult result)
    {
        if (result.TotalDamage > 0)
            return result.TotalDamage;

        return result.UnblockedDamage;
    }

    private sealed record RecentDamageRecord(
        long TimestampTicks,
        string ReceiverName,
        string DealerName,
        string CardName,
        int TotalDamage,
        int UnblockedDamage,
        int BlockedDamage,
        int OverkillDamage);

    private sealed record PendingVictoryHit(
        long TimestampTicks,
        string PlayerId,
        string PlayerName,
        string TargetName,
        string DealerName,
        string CardName,
        int TargetHp,
        int TargetBlock);

    private sealed record PendingDoomKill(Creature Receiver, int PreHp, int PreBlock);

    private sealed record CachedPoisonSource(long TimestampTicks, Creature Receiver, Creature Dealer);

    private sealed record CachedRecentDealer(long TimestampTicks, Creature Dealer);
}
