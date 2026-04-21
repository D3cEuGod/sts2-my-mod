using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Sts2DpsPrototype;

internal sealed class DpsCombatHook : AbstractModel
{
    public DpsCombatHook()
    {
    }

    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCombatStart()
    {
        DpsTracker.BeginCombat();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        DpsTracker.EndCombat();
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer == null)
            return Task.CompletedTask;

        Player? player = ResolveOwningPlayer(dealer);
        if (player == null)
            return Task.CompletedTask;

        int damage = GetTrackedDamage(result);
        if (damage <= 0)
            return Task.CompletedTask;

        string playerId = player.NetId.ToString();
        string playerName = ResolvePlayerName(player);
        DpsTracker.RecordDamage(playerId, playerName, damage);
        return Task.CompletedTask;
    }

    private static Player? ResolveOwningPlayer(Creature dealer)
    {
        if (dealer.Player != null)
            return dealer.Player;

        if (dealer.PetOwner != null)
            return dealer.PetOwner;

        return null;
    }

    private static string ResolvePlayerName(Player player)
    {
        if (!string.IsNullOrWhiteSpace(player.Creature?.Name))
            return player.Creature.Name;

        string? characterName = player.Character?.Title?.ToString();
        if (!string.IsNullOrWhiteSpace(characterName))
            return characterName;

        return $"Player {player.NetId}";
    }

    private static int GetTrackedDamage(DamageResult result)
    {
        int effectiveDamage = result.TotalDamage - result.OverkillDamage;
        if (effectiveDamage > 0)
            return effectiveDamage;

        return result.UnblockedDamage;
    }
}
