namespace Sts2DpsPrototype;

internal static class DpsTracker
{
    private static readonly Dictionary<string, PlayerDamageState> Players = new();
    private static readonly Dictionary<string, LifetimeDamageState> LifetimePlayers = new();
    private static readonly List<CombatRecord> CombatHistoryRecords = new();
    private static IReadOnlyList<PlayerSnapshot> _publishedCombatSnapshots = Array.Empty<PlayerSnapshot>();
    private static IReadOnlyList<PlayerSnapshot> _previousCombatSnapshots = Array.Empty<PlayerSnapshot>();
    private static int _currentRoundNumber;
    private static int _publishedCombatRoundCount;
    private static bool _combatActive;
    private static bool _combatSeen;
    private static bool _combatFinalizing;
    private static bool _publishedCombatSeen;
    private static bool _previousCombatSeen;

    internal static void Tick(double delta)
    {
        FinalizeCombatIfPending();
    }

    internal static void BeginCombat(IEnumerable<PlayerSeed>? roster = null)
    {
        FinalizeCombatIfPending();

        Players.Clear();
        _combatActive = true;
        _combatSeen = true;
        _combatFinalizing = false;
        _currentRoundNumber = 1;

        if (roster == null)
            return;

        int sortOrder = 0;
        foreach (var player in roster)
        {
            SeedPlayer(player.PlayerId, player.DisplayName, sortOrder);
            sortOrder++;
        }
    }

    internal static void SetRoundNumber(int roundNumber)
    {
        _currentRoundNumber = Math.Max(1, roundNumber);
    }

    internal static void EndCombat()
    {
        if (!_combatSeen)
            return;

        _combatActive = false;
        _combatFinalizing = true;
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

        if (!_combatActive && !_combatFinalizing)
            BeginCombat();

        if (!Players.TryGetValue(playerId, out var state))
        {
            state = SeedPlayer(playerId, playerName, Players.Count);
        }

        state.DisplayName = string.IsNullOrWhiteSpace(playerName) ? state.DisplayName : playerName;
        state.TotalDamage += damage;
        state.HighestSingleHit = Math.Max(state.HighestSingleHit, damage);
    }

    internal static IReadOnlyList<PlayerSnapshot> GetSnapshots(int maxRows)
    {
        if (_combatActive)
            return BuildLiveSnapshots(maxRows);

        return _publishedCombatSnapshots.Take(maxRows).ToArray();
    }

    internal static string GetEncounterSummary()
    {
        if (_combatActive)
        {
            var liveSnapshots = BuildLiveSnapshots(int.MaxValue);
            if (liveSnapshots.Count == 0)
                return "战斗进行中，暂时还没有有效伤害。";

            float liveTotalDamage = liveSnapshots.Sum(player => player.TotalDamage);
            int activeDealers = liveSnapshots.Count(player => player.TotalDamage > 0f);
            return $"战斗进行中 · 第 {GetEffectiveRoundCount()} 回合 · 出伤 {activeDealers}/{liveSnapshots.Count} 人 · 总伤害 {liveTotalDamage:F0}";
        }

        if (!_publishedCombatSeen)
            return _combatSeen
                ? "本场已结束，但还没有可展示的结算数据。"
                : "还没有已结算战斗，开打后会自动开始统计。";

        float totalDamage = _publishedCombatSnapshots.Sum(player => player.TotalDamage);
        int finishedDealers = _publishedCombatSnapshots.Count(player => player.TotalDamage > 0f);
        return $"本场结算 · {_publishedCombatRoundCount} 回合 · 出伤 {finishedDealers}/{_publishedCombatSnapshots.Count} 人 · 总伤害 {totalDamage:F0}";
    }

    internal static string GetLifetimeSummary()
    {
        float totalDamage = LifetimePlayers.Values.Sum(player => player.TotalDamage);
        if (_combatActive)
            totalDamage += Players.Values.Sum(player => player.TotalDamage);

        if (totalDamage <= 0f)
            return "当前这一局还没有累计到有效伤害。";

        return $"当前这一局累计总伤害 {totalDamage:F0}";
    }

    internal static string GetLastCombatSummary()
    {
        var snapshots = GetVisibleLastCombatSnapshots();
        if (snapshots.Count == 0)
            return "还没有上一场可展示的结算。";

        float totalDamage = snapshots.Sum(snapshot => snapshot.TotalDamage);
        int activeDealers = snapshots.Count(snapshot => snapshot.TotalDamage > 0f);
        return $"上一场总伤害 {totalDamage:F0} · 出伤 {activeDealers} 人";
    }

    internal static IReadOnlyList<PlayerSnapshot> GetLifetimeSnapshots(int maxRows)
    {
        var combined = LifetimePlayers.Values.ToDictionary(
            state => state.PlayerId,
            state => new LifetimeDamageState(state.PlayerId, state.DisplayName, state.SortOrder)
            {
                TotalDamage = state.TotalDamage
            });

        if (_combatActive)
        {
            foreach (var state in Players.Values)
            {
                if (!combined.TryGetValue(state.PlayerId, out var lifetime))
                {
                    lifetime = new LifetimeDamageState(state.PlayerId, state.DisplayName, state.SortOrder);
                    combined[state.PlayerId] = lifetime;
                }

                lifetime.DisplayName = state.DisplayName;
                lifetime.TotalDamage += state.TotalDamage;
            }
        }

        if (combined.Count == 0)
            return Array.Empty<PlayerSnapshot>();

        return combined.Values
            .Where(state => state.TotalDamage > 0f)
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
        return GetVisibleLastCombatSnapshots()
            .Where(snapshot => snapshot.TotalDamage > 0f)
            .Take(maxRows)
            .ToArray();
    }

    internal static string GetCombatHistorySummary()
    {
        int recordCount = GetHistoricalCombatRecords().Count;
        if (recordCount == 0)
            return "还没有更早的战斗记录。";

        return $"本局已保留 {recordCount} 场更早战斗记录";
    }

    internal static IReadOnlyList<CombatRecord> GetHistoricalCombatRecords()
    {
        if (CombatHistoryRecords.Count == 0)
            return Array.Empty<CombatRecord>();

        int visibleCount = CombatHistoryRecords.Count;
        if (!_combatActive && _publishedCombatSeen)
            visibleCount--;

        if (visibleCount <= 0)
            return Array.Empty<CombatRecord>();

        return CombatHistoryRecords
            .Take(visibleCount)
            .Reverse()
            .ToArray();
    }

    internal static void Reset()
    {
        Players.Clear();
        LifetimePlayers.Clear();
        CombatHistoryRecords.Clear();
        _combatActive = false;
        _combatSeen = false;
        _combatFinalizing = false;
        _publishedCombatSeen = false;
        _previousCombatSeen = false;
        _publishedCombatSnapshots = Array.Empty<PlayerSnapshot>();
        _previousCombatSnapshots = Array.Empty<PlayerSnapshot>();
        _currentRoundNumber = 1;
        _publishedCombatRoundCount = 0;
    }

    internal static void ResetForNewRun()
    {
        Reset();
        MainFile.Log.Info("[RUN_RESET] Cleared combat, last-combat, and run-total damage state for a new run.");
    }

    private static IReadOnlyList<PlayerSnapshot> BuildLiveSnapshots(int maxRows)
    {
        if (Players.Count == 0)
            return Array.Empty<PlayerSnapshot>();

        int roundCount = GetEffectiveRoundCount();

        return Players.Values
            .Select(state => new PlayerSnapshot(
                state.PlayerId,
                state.DisplayName,
                state.TotalDamage,
                state.TotalDamage / roundCount,
                state.HighestSingleHit,
                state.SortOrder))
            .OrderByDescending(snapshot => snapshot.TotalDamage)
            .ThenBy(snapshot => snapshot.SortOrder)
            .Take(maxRows)
            .ToArray();
    }

    private static IReadOnlyList<PlayerSnapshot> GetVisibleLastCombatSnapshots()
    {
        if (_combatActive)
            return _publishedCombatSnapshots;

        if (_previousCombatSeen && _previousCombatSnapshots.Count > 0)
            return _previousCombatSnapshots;

        return Array.Empty<PlayerSnapshot>();
    }

    private static int GetEffectiveRoundCount()
    {
        return Math.Max(1, _currentRoundNumber);
    }

    private static void FinalizeCombatIfPending()
    {
        if (_combatActive || !_combatFinalizing)
            return;

        _combatFinalizing = false;

        var finishedSnapshots = BuildLiveSnapshots(int.MaxValue).ToArray();
        if (finishedSnapshots.Length == 0)
            return;

        if (_publishedCombatSeen)
        {
            _previousCombatSnapshots = _publishedCombatSnapshots.ToArray();
            _previousCombatSeen = _previousCombatSnapshots.Count > 0;
        }

        _publishedCombatSnapshots = finishedSnapshots;
        _publishedCombatRoundCount = GetEffectiveRoundCount();
        _publishedCombatSeen = true;

        CombatHistoryRecords.Add(new CombatRecord(
            CombatHistoryRecords.Count + 1,
            _publishedCombatRoundCount,
            finishedSnapshots));

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

        MainFile.Log.Info($"[COMBAT_FINALIZED] rounds={_publishedCombatRoundCount} players={finishedSnapshots.Length} total={finishedSnapshots.Sum(player => player.TotalDamage):F0}");
    }

    private static PlayerDamageState SeedPlayer(string playerId, string playerName, int sortOrder)
    {
        var state = new PlayerDamageState(
            playerId,
            string.IsNullOrWhiteSpace(playerName) ? $"Player {sortOrder + 1}" : playerName,
            sortOrder);
        Players[playerId] = state;
        return state;
    }

    internal sealed class PlayerDamageState
    {
        internal PlayerDamageState(string playerId, string displayName, int sortOrder)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            SortOrder = sortOrder;
        }

        internal string PlayerId { get; }
        internal string DisplayName { get; set; }
        internal float TotalDamage { get; set; }
        internal float HighestSingleHit { get; set; }
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
        float DamagePerTurn,
        float HighestSingleHit,
        int SortOrder);

    internal sealed record CombatRecord(
        int CombatIndex,
        int RoundCount,
        IReadOnlyList<PlayerSnapshot> Snapshots)
    {
        internal float TotalDamage => Snapshots.Sum(snapshot => snapshot.TotalDamage);
        internal int ActiveDealers => Snapshots.Count(snapshot => snapshot.TotalDamage > 0f);
        internal float HighestSingleHit => Snapshots.Count == 0 ? 0f : Snapshots.Max(snapshot => snapshot.HighestSingleHit);
    }
}
