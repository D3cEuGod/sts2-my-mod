using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Sts2DpsPrototype;

internal static class DamageDebugPatches
{
    private static bool _patched;
    private static readonly List<RecentModifierLog> RecentModifierLogs = new();
    private static readonly List<RecentEventLog> RecentEventLogs = new();

    internal static void EnsurePatched()
    {
        if (_patched)
            return;

        var harmony = new Harmony($"{MainFile.ModId}.damage-debug");
        harmony.Patch(
            original: AccessTools.Method(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative)),
            postfix: new HarmonyMethod(typeof(DamageDebugPatches), nameof(OnWeakModifyDamageMultiplicative)));
        harmony.Patch(
            original: AccessTools.Method(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative)),
            postfix: new HarmonyMethod(typeof(DamageDebugPatches), nameof(OnVulnerableModifyDamageMultiplicative)));
        harmony.Patch(
            original: AccessTools.Method(typeof(ThornsPower), nameof(ThornsPower.BeforeDamageReceived)),
            prefix: new HarmonyMethod(typeof(DamageDebugPatches), nameof(OnThornsBeforeDamageReceived)));
        harmony.Patch(
            original: AccessTools.Method(typeof(ReflectPower), nameof(ReflectPower.AfterDamageReceived)),
            postfix: new HarmonyMethod(typeof(DamageDebugPatches), nameof(OnReflectAfterDamageReceived)));
        harmony.Patch(
            original: AccessTools.Method(typeof(OstyCmd), nameof(OstyCmd.Summon)),
            prefix: new HarmonyMethod(typeof(DamageDebugPatches), nameof(OnOstySummon)));

        _patched = true;
        MainFile.Log.Info("Patched damage debug hooks for weak/vulnerable/thorns/reflect/osty validation.");
    }

    private static void OnWeakModifyDamageMultiplicative(Creature target, decimal amount, Creature dealer, CardModel? cardSource, decimal __result)
    {
        LogDamageModifier("WEAK_MOD", dealer, target, cardSource, amount, __result);
    }

    private static void OnVulnerableModifyDamageMultiplicative(Creature target, decimal amount, Creature dealer, CardModel? cardSource, decimal __result)
    {
        LogDamageModifier("VULN_MOD", dealer, target, cardSource, amount, __result);
    }

    private static void OnThornsBeforeDamageReceived(Creature target, decimal amount, Creature dealer, CardModel? cardSource)
    {
        string dealerName = DescribeCreature(dealer);
        string targetName = DescribeCreature(target);
        string cardName = DescribeCard(cardSource);
        string payload = $"amount={amount:0.##}";
        if (WasRecentlyLoggedEvent("THORNS_PRE", dealerName, targetName, cardName, payload))
            return;

        MainFile.Log.Info($"[THORNS_PRE] dealer={dealerName} target={targetName} amount={amount:0.##} card={cardName}");
    }

    private static void OnReflectAfterDamageReceived(Creature target, DamageResult result, Creature dealer, CardModel? cardSource)
    {
        string dealerName = DescribeCreature(dealer);
        string targetName = DescribeCreature(target);
        string cardName = DescribeCard(cardSource);
        string payload = $"total={result.TotalDamage}|unblocked={result.UnblockedDamage}|blocked={result.BlockedDamage}|overkill={result.OverkillDamage}";
        if (WasRecentlyLoggedEvent("REFLECT_POST", dealerName, targetName, cardName, payload))
            return;

        MainFile.Log.Info(
            $"[REFLECT_POST] dealer={dealerName} target={targetName} total={result.TotalDamage} unblocked={result.UnblockedDamage} blocked={result.BlockedDamage} overkill={result.OverkillDamage} card={cardName}");
    }

    private static void OnOstySummon(Player summoner, decimal amount, AbstractModel? source)
    {
        string summonerName = DescribePlayer(summoner);
        string sourceName = source?.GetType().Name ?? "<null>";
        string payload = $"amount={amount:0.##}";
        if (WasRecentlyLoggedEvent("OSTY_SUMMON", summonerName, "<none>", sourceName, payload))
            return;

        MainFile.Log.Info($"[OSTY_SUMMON] summoner={summonerName} amount={amount:0.##} source={sourceName}");
    }

    private static void LogDamageModifier(string tag, Creature dealer, Creature target, CardModel? cardSource, decimal inputAmount, decimal multiplier)
    {
        if (multiplier == 1m)
            return;

        string dealerName = DescribeCreature(dealer);
        string targetName = DescribeCreature(target);
        string cardName = DescribeCard(cardSource);
        if (WasRecentlyLogged(tag, dealerName, targetName, cardName, inputAmount, multiplier))
            return;

        decimal estimatedFinal = inputAmount * multiplier;
        decimal estimatedDelta = estimatedFinal - inputAmount;
        MainFile.Log.Info(
            $"[{tag}] dealer={dealerName} target={targetName} card={cardName} input={inputAmount:0.##} multiplier={multiplier:0.###} estimatedFinal={estimatedFinal:0.##} estimatedDelta={estimatedDelta:+0.##;-0.##;0}");
    }

    private static bool WasRecentlyLogged(string tag, string dealerName, string targetName, string cardName, decimal inputAmount, decimal multiplier)
    {
        long now = DateTime.UtcNow.Ticks;
        long cutoff = now - TimeSpan.FromSeconds(2).Ticks;
        RecentModifierLogs.RemoveAll(entry => entry.TimestampTicks < cutoff);

        bool alreadyLogged = RecentModifierLogs.Any(entry =>
            entry.Tag == tag
            && entry.DealerName == dealerName
            && entry.TargetName == targetName
            && entry.CardName == cardName
            && entry.InputAmount == inputAmount
            && entry.Multiplier == multiplier);
        if (alreadyLogged)
            return true;

        RecentModifierLogs.Add(new RecentModifierLog(now, tag, dealerName, targetName, cardName, inputAmount, multiplier));
        return false;
    }

    private static bool WasRecentlyLoggedEvent(string tag, string actorName, string targetName, string sourceName, string payload)
    {
        long now = DateTime.UtcNow.Ticks;
        long cutoff = now - TimeSpan.FromSeconds(2).Ticks;
        RecentEventLogs.RemoveAll(entry => entry.TimestampTicks < cutoff);

        bool alreadyLogged = RecentEventLogs.Any(entry =>
            entry.Tag == tag
            && entry.ActorName == actorName
            && entry.TargetName == targetName
            && entry.SourceName == sourceName
            && entry.Payload == payload);
        if (alreadyLogged)
            return true;

        RecentEventLogs.Add(new RecentEventLog(now, tag, actorName, targetName, sourceName, payload));
        return false;
    }

    private static string DescribeCreature(Creature? creature)
    {
        if (creature == null)
            return "<null>";

        string name = !string.IsNullOrWhiteSpace(creature.Name) ? creature.Name : creature.GetType().Name;
        Player? owner = creature.Player ?? creature.PetOwner;
        if (owner != null)
            return $"{name}[owner={DescribePlayer(owner)}]";

        return name;
    }

    private static string DescribePlayer(Player player)
    {
        string name = !string.IsNullOrWhiteSpace(player.Creature?.Name)
            ? player.Creature.Name
            : player.Character?.Title?.ToString() ?? $"Player {player.NetId}";
        return $"{name}#{player.NetId}";
    }

    private static string DescribeCard(CardModel? cardSource)
    {
        return cardSource?.GetType().Name ?? "<null>";
    }

    private sealed record RecentModifierLog(
        long TimestampTicks,
        string Tag,
        string DealerName,
        string TargetName,
        string CardName,
        decimal InputAmount,
        decimal Multiplier);

    private sealed record RecentEventLog(
        long TimestampTicks,
        string Tag,
        string ActorName,
        string TargetName,
        string SourceName,
        string Payload);
}
