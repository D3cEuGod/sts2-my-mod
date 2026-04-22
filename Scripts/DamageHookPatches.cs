using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Sts2DpsPrototype;

internal static class DamageHookPatches
{
    private static bool _patched;

    internal static void EnsurePatched()
    {
        if (_patched)
            return;

        var harmony = new Harmony($"{MainFile.ModId}.harmony");
        harmony.Patch(
            original: AccessTools.Method(typeof(CombatHistory), nameof(CombatHistory.DamageReceived)),
            postfix: new HarmonyMethod(typeof(DamageHookPatches), nameof(OnDamageReceived)));

        _patched = true;
        MainFile.Log.Info("Patched CombatHistory.DamageReceived for DPS tracking.");
    }

    private static void OnDamageReceived(CombatState combatState, Creature receiver, Creature dealer, DamageResult result, CardModel? cardSource)
    {
        Player? owner = ResolveOwningPlayer(dealer);
        if (owner == null)
            return;

        int damage = GetTrackedDamage(result);
        if (damage <= 0)
            return;

        string playerId = owner.NetId.ToString();
        string playerName = CombatRuntimeBridge.ResolveDisplayName(combatState, owner);
        string targetName = receiver?.Name ?? receiver?.GetType().Name ?? "<null>";
        string dealerName = dealer?.Name ?? dealer?.GetType().Name ?? "<null>";
        string cardName = cardSource?.GetType().Name ?? "<null>";
        string petOwner = dealer?.PetOwner?.NetId.ToString() ?? "<none>";
        MainFile.Log.Info($"[DAMAGE_FINAL] owner={playerName} target={targetName} dealer={dealerName} card={cardName} total={result.TotalDamage} unblocked={result.UnblockedDamage} blocked={result.BlockedDamage} overkill={result.OverkillDamage} tracked={damage} petOwner={petOwner}");
        DpsTracker.RecordDamage(playerId, playerName, damage);
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

    private static int GetTrackedDamage(DamageResult result)
    {
        int effectiveDamage = result.TotalDamage - result.OverkillDamage;
        if (effectiveDamage > 0)
            return effectiveDamage;

        return result.UnblockedDamage;
    }
}
