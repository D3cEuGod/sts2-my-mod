using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2DpsPrototype;

internal sealed class CombatRuntimeBridge
{
    private CombatManager? _combatManager;
    private IReadOnlyList<DpsTracker.PlayerSeed> _currentRoster = Array.Empty<DpsTracker.PlayerSeed>();

    internal void Update()
    {
        var manager = CombatManager.Instance;
        if (manager == null)
            return;

        if (!ReferenceEquals(_combatManager, manager))
        {
            UnsubscribeManager();
            _combatManager = manager;
            _combatManager.CombatSetUp += OnCombatSetUp;
            _combatManager.CombatEnded += OnCombatEnded;
            _combatManager.TurnStarted += OnTurnStarted;
        }

        SyncCurrentCombat(manager);
    }

    internal void Dispose()
    {
        UnsubscribeManager();
    }

    internal void ResetTrackedCombat()
    {
        DpsTracker.Reset();

        if (_combatManager?.IsInProgress == true && _currentRoster.Count > 0)
            DpsTracker.BeginCombat(_currentRoster);
    }

    private void UnsubscribeManager()
    {
        if (_combatManager == null)
            return;

        _combatManager.CombatSetUp -= OnCombatSetUp;
        _combatManager.CombatEnded -= OnCombatEnded;
        _combatManager.TurnStarted -= OnTurnStarted;
        _combatManager = null;
    }

    private void SyncCurrentCombat(CombatManager manager)
    {
        if (!manager.IsInProgress)
            return;

        CombatState? state = manager.DebugOnlyGetState();
        if (state == null)
            return;

        DpsTracker.SetRoundNumber(state.RoundNumber);

        if (_currentRoster.Count > 0 && DpsTracker.HasRoster(_currentRoster))
            return;

        OnCombatSetUp(state);
    }

    private void OnCombatSetUp(CombatState state)
    {
        _currentRoster = BuildCombatRoster(state);
        DpsTracker.BeginCombat(_currentRoster);
        DpsTracker.SetRoundNumber(state.RoundNumber);
    }

    private void OnCombatEnded(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        DpsTracker.EndCombat();
    }

    private void OnTurnStarted(CombatState state)
    {
        DpsTracker.SetRoundNumber(state.RoundNumber);
    }

    private IReadOnlyList<DpsTracker.PlayerSeed> BuildCombatRoster(CombatState state)
    {
        var players = state.Players.ToList();
        bool showSlots = players.Count > 1;
        var roster = new List<DpsTracker.PlayerSeed>(players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            string label = ResolveBasePlayerName(player, i);
            if (showSlots)
                label = $"P{i + 1} {label}";

            roster.Add(new DpsTracker.PlayerSeed(player.NetId.ToString(), label));
        }

        return roster;
    }

    internal static string ResolveDisplayName(CombatState state, Player player)
    {
        int slotIndex = ResolvePlayerSlotIndex(state.RunState, player);
        string label = ResolveBasePlayerName(player, slotIndex);
        if (state.Players.Count > 1)
            return $"P{slotIndex + 1} {label}";

        return label;
    }

    private static int ResolvePlayerSlotIndex(IRunState runState, Player player)
    {
        int slotIndex = runState.GetPlayerSlotIndex(player);
        return slotIndex >= 0 ? slotIndex : 0;
    }

    private static string ResolveBasePlayerName(Player player, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(player.Creature?.Name))
            return player.Creature.Name;

        string? characterName = player.Character?.Title?.ToString();
        if (!string.IsNullOrWhiteSpace(characterName))
            return characterName;

        return $"Player {fallbackIndex + 1}";
    }

}
