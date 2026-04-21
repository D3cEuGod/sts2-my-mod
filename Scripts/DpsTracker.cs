namespace Sts2DpsPrototype;

internal static class DpsTracker
{
    private const double EncounterTimeoutSeconds = 8.0;

    private static readonly Dictionary<string, PlayerDamageState> Players = new();
    private static readonly Dictionary<string, LifetimeDamageState> LifetimePlayers = new();
    private static IReadOnlyList<PlayerSnapshot> _publishedCombatSnapshots = Array.Empty<PlayerSnapshot>();
    private static IReadOnlyList<PlayerSnapshot> _previousCombatSnapshots = Array.Empty<PlayerSnapshot>();
    private static double _publishedCombatDurationSeconds;
    private static double _clockSeconds;
    private static double _combatStartSeconds;
    private static double _lastDamageSeconds;
    private static bool _combatActive;
    private static bool _combatSeen;
    private static bool _publishedCombatSeen;
    private static bool _previousCombatSeen;

    internal static void Tick(double delta)
    {
        _clockSeconds += delta;

        if (_combatActive && _clockSeconds - _lastDamageSeconds >= EncounterTimeoutSeconds)
            EndCombat();
    }

    internal static void BeginCombat(IEnumerable<PlayerSeed>? roster = null)
    {
        Players.Clear();
        _combatActive = true;
        _combatSeen = true;
        _combatStartSeconds = _clockSeconds;
        _lastDamageSeconds = _clockSeconds;

        if (roster == null)
            return;

        int sortOrder = 0;
        foreach (var player in roster)
        {
            SeedPlayer(player.PlayerId, player.DisplayName, sortOrder);
            sortOrder++;
        }
    }

    internal static void EndCombat()
    {
        if (!_combatSeen)
            return;

        _combatActive = false;
        _lastDamageSeconds = _clockSeconds;

        var finishedSnapshots = BuildLiveSnapshots(int.MaxValue).ToArray();
        if (finishedSnapshots.Length == 0)
            return;

        if (_publishedCombatSeen)
        {
            _previousCombatSnapshots = _publishedCombatSnapshots.ToArray();
            _previousCombatSeen = _previousCombatSnapshots.Count > 0;
        }

        _publishedCombatSnapshots = finishedSnapshots;
        _publishedCombatDurationSeconds = GetEncounterDurationSeconds();
        _publishedCombatSeen = true;

        foreach (var state in Players.Values)
        {
            if (state.TotalDamage <= 0f)
                continue;

            if (!LifetimePlayers.TryGetValue(state.PlayerId, out var lifetime))
            {
                lifetime = new LifetimeDamageState(state.PlayerId, state.DisplayName, state.SortOrder);
                LifetimePlayers[state.PlayerId] = lifetime;
            }

            lifetime.DisplayName = state.DisplayName;
            lifetime.TotalDamage += state.TotalDamage;
        }
    }

    internal static bool HasRoster(IReadOnlyList<PlayerSeed> roster)
    {
        if (roster.Count == 0 || Players.Count != roster.Count)
            return false;

        return roster.All(player => Players.ContainsKey(player.PlayerId));
    }

    internal static void RecordDamage(string playerId, string playerName, float damage)
    {
        if (string.IsNullOrWhiteSpace(playerId) || damage <= 0f)
            return;

        if (!_combatActive)
            BeginCombat();

        _lastDamageSeconds = _clockSeconds;

        if (!Players.TryGetValue(playerId, out var state))
        {
            state = SeedPlayer(playerId, playerName, Players.Count);
        }

        state.DisplayName = string.IsNullOrWhiteSpace(playerName) ? state.DisplayName : playerName;
        state.TotalDamage += damage;
        state.LastDamageSeconds = _clockSeconds;
    }

    internal static IReadOnlyList<PlayerSnapshot> GetSnapshots(int maxRows)
    {
        return _publishedCombatSnapshots.Take(maxRows).ToArray();
    }

    internal static string GetEncounterSummary()
    {
        if (_combatActive)
            return "战斗进行中，面板会在本场结算后刷新。";

        if (!_publishedCombatSeen)
            return _combatSeen
                ? "本场已结束，但还没有可展示的结算数据。"
                : "还没有已结算战斗，开打后会在战斗结束时刷新。";

        float totalDamage = _publishedCombatSnapshots.Sum(player => player.TotalDamage);
        int activeDealers = _publishedCombatSnapshots.Count(player => player.TotalDamage > 0f);
        return $"本场结算 · {_publishedCombatDurationSeconds:F1}s · 出伤 {activeDealers}/{_publishedCombatSnapshots.Count} 人 · 总伤害 {totalDamage:F0}";
    }

    internal static string GetLifetimeSummary()
    {
        if (LifetimePlayers.Count == 0)
            return _combatActive
                ? "累计数据会在本场战斗结算后更新。"
                : "本次启动后还没有累计到有效伤害。";

        float totalDamage = LifetimePlayers.Values.Sum(player => player.TotalDamage);
        return $"本次启动累计总伤害 {totalDamage:F0}";
    }

    internal static string GetLastCombatSummary()
    {
        if (!_previousCombatSeen || _previousCombatSnapshots.Count == 0)
            return "还没有上一场可展示的结算。";

        float totalDamage = _previousCombatSnapshots.Sum(snapshot => snapshot.TotalDamage);
        return $"上一场总伤害 {totalDamage:F0} · 出伤 {_previousCombatSnapshots.Count} 人";
    }

    internal static IReadOnlyList<PlayerSnapshot> GetLifetimeSnapshots(int maxRows)
    {
        if (LifetimePlayers.Count == 0)
            return Array.Empty<PlayerSnapshot>();

        return LifetimePlayers.Values
            .Select(state => new PlayerSnapshot(
                state.PlayerId,
                state.DisplayName,
                state.TotalDamage,
                0f,
                0f,
                state.SortOrder))
            .OrderByDescending(snapshot => snapshot.TotalDamage)
            .ThenBy(snapshot => snapshot.SortOrder)
            .Take(maxRows)
            .ToArray();
    }

    internal static IReadOnlyList<PlayerSnapshot> GetLastCombatSnapshots(int maxRows)
    {
        return _previousCombatSnapshots.Take(maxRows).ToArray();
    }

    internal static void Reset()
    {
        Players.Clear();
        LifetimePlayers.Clear();
        _combatActive = false;
        _combatSeen = false;
        _publishedCombatSeen = false;
        _previousCombatSeen = false;
        _publishedCombatSnapshots = Array.Empty<PlayerSnapshot>();
        _previousCombatSnapshots = Array.Empty<PlayerSnapshot>();
        _publishedCombatDurationSeconds = 0d;
        _combatStartSeconds = _clockSeconds;
        _lastDamageSeconds = _clockSeconds;
    }

    private static IReadOnlyList<PlayerSnapshot> BuildLiveSnapshots(int maxRows)
    {
        if (Players.Count == 0)
            return Array.Empty<PlayerSnapshot>();

        double encounterDuration = GetEncounterDurationSeconds();

        return Players.Values
            .Select(state => new PlayerSnapshot(
                state.PlayerId,
                state.DisplayName,
                state.TotalDamage,
                (float)(state.TotalDamage / encounterDuration),
                (float)(_clockSeconds - state.LastDamageSeconds),
                state.SortOrder))
            .OrderByDescending(snapshot => snapshot.TotalDamage)
            .ThenBy(snapshot => snapshot.SortOrder)
            .Take(maxRows)
            .ToArray();
    }

    private static PlayerDamageState SeedPlayer(string playerId, string playerName, int sortOrder)
    {
        var state = new PlayerDamageState(
            playerId,
            string.IsNullOrWhiteSpace(playerName) ? $"Player {sortOrder + 1}" : playerName,
            _clockSeconds,
            sortOrder);
        Players[playerId] = state;
        return state;
    }

    private static double GetEncounterDurationSeconds()
    {
        return Math.Max(1.0, (_combatActive ? _clockSeconds : _lastDamageSeconds) - _combatStartSeconds);
    }

    internal sealed class PlayerDamageState
    {
        internal PlayerDamageState(string playerId, string displayName, double firstDamageSeconds, int sortOrder)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            FirstDamageSeconds = firstDamageSeconds;
            LastDamageSeconds = firstDamageSeconds;
            SortOrder = sortOrder;
        }

        internal string PlayerId { get; }
        internal string DisplayName { get; set; }
        internal float TotalDamage { get; set; }
        internal double FirstDamageSeconds { get; }
        internal double LastDamageSeconds { get; set; }
        internal int SortOrder { get; }
    }

    internal sealed class LifetimeDamageState
    {
        internal LifetimeDamageState(string playerId, string displayName, int sortOrder)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            SortOrder = sortOrder;
        }

        internal string PlayerId { get; }
        internal string DisplayName { get; set; }
        internal float TotalDamage { get; set; }
        internal int SortOrder { get; }
    }

    internal sealed record PlayerSeed(
        string PlayerId,
        string DisplayName);

    internal sealed record PlayerSnapshot(
        string PlayerId,
        string DisplayName,
        float TotalDamage,
        float Dps,
        float SecondsSinceLastHit,
        int SortOrder);
}
