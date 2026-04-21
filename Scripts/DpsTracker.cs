namespace Sts2DpsPrototype;

internal static class DpsTracker
{
    private const double EncounterTimeoutSeconds = 8.0;

    private static readonly Dictionary<string, PlayerDamageState> Players = new();
    private static readonly Dictionary<string, LifetimeDamageState> LifetimePlayers = new();
    private static IReadOnlyList<PlayerSnapshot> _lastCombatSnapshots = Array.Empty<PlayerSnapshot>();
    private static double _clockSeconds;
    private static double _combatStartSeconds;
    private static double _lastDamageSeconds;
    private static bool _combatActive;
    private static bool _combatSeen;
    private static bool _lastCombatSeen;

    internal static void Tick(double delta)
    {
        _clockSeconds += delta;

        if (_combatActive && _clockSeconds - _lastDamageSeconds >= EncounterTimeoutSeconds)
            _combatActive = false;
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
        _combatActive = false;
        _lastDamageSeconds = _clockSeconds;
        _lastCombatSnapshots = GetSnapshots(int.MaxValue).ToArray();
        _lastCombatSeen = _lastCombatSnapshots.Count > 0;
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

        if (!LifetimePlayers.TryGetValue(playerId, out var lifetime))
        {
            lifetime = new LifetimeDamageState(playerId, state.DisplayName, state.SortOrder);
            LifetimePlayers[playerId] = lifetime;
        }

        lifetime.DisplayName = state.DisplayName;
        lifetime.TotalDamage += damage;
    }

    internal static IReadOnlyList<PlayerSnapshot> GetSnapshots(int maxRows)
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

    internal static string GetEncounterSummary()
    {
        if (!_combatSeen)
            return "未进入战斗，开打后会自动开始统计。";

        if (Players.Count == 0)
            return _combatActive
                ? "战斗进行中，暂时还没有有效伤害。"
                : "本场已结束，但没有记录到有效伤害。";

        double duration = GetEncounterDurationSeconds();
        float totalDamage = Players.Values.Sum(player => player.TotalDamage);
        int activeDealers = Players.Values.Count(player => player.TotalDamage > 0f);
        string state = _combatActive ? "进行中" : "已结算";
        return $"{state} · {duration:F1}s · 出伤 {activeDealers}/{Players.Count} 人 · 总伤害 {totalDamage:F0}";
    }

    internal static string GetLifetimeSummary()
    {
        if (LifetimePlayers.Count == 0)
            return "本次启动后还没有累计到有效伤害。";

        float totalDamage = LifetimePlayers.Values.Sum(player => player.TotalDamage);
        return $"本次启动累计总伤害 {totalDamage:F0}";
    }

    internal static string GetLastCombatSummary()
    {
        if (!_lastCombatSeen || _lastCombatSnapshots.Count == 0)
            return "还没有可展示的上一场结算。";

        float totalDamage = _lastCombatSnapshots.Sum(snapshot => snapshot.TotalDamage);
        return $"上一场总伤害 {totalDamage:F0} · 出伤 {_lastCombatSnapshots.Count} 人";
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
        return _lastCombatSnapshots.Take(maxRows).ToArray();
    }

    internal static void Reset()
    {
        Players.Clear();
        _combatActive = false;
        _combatSeen = false;
        _lastCombatSeen = false;
        _lastCombatSnapshots = Array.Empty<PlayerSnapshot>();
        _combatStartSeconds = _clockSeconds;
        _lastDamageSeconds = _clockSeconds;
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
