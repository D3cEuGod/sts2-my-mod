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
        MainFile.Log.Info($"[THORNS_PRE] dealer={DescribeCreature(dealer)} target={DescribeCreature(target)} amount={amount:0.##} card={DescribeCard(cardSource)}");
    }

    private static void OnReflectAfterDamageReceived(Creature target, DamageResult result, Creature dealer, CardModel? cardSource)
    {
        MainFile.Log.Info(
            $"[REFLECT_POST] dealer={DescribeCreature(dealer)} target={DescribeCreature(target)} total={result.TotalDamage} unblocked={result.UnblockedDamage} blocked={result.BlockedDamage} overkill={result.OverkillDamage} card={DescribeCard(cardSource)}");
    }

    private static void OnOstySummon(Player summoner, decimal amount, AbstractModel? source)
    {
        MainFile.Log.Info($"[OSTY_SUMMON] summoner={DescribePlayer(summoner)} amount={amount:0.##} source={source?.GetType().Name ?? "<null>"}");
    }

    private static void LogDamageModifier(string tag, Creature dealer, Creature target, CardModel? cardSource, decimal originalAmount, decimal modifiedAmount)
    {
        if (originalAmount == modifiedAmount)
            return;

        decimal delta = modifiedAmount - originalAmount;
        MainFile.Log.Info(
            $"[{tag}] dealer={DescribeCreature(dealer)} target={DescribeCreature(target)} card={DescribeCard(cardSource)} base={originalAmount:0.##} modified={modifiedAmount:0.##} delta={delta:+0.##;-0.##;0}");
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
}
