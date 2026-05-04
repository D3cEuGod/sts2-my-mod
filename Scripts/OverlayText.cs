using Godot;

namespace Sts2DpsPrototype;

internal enum OverlayLanguageMode
{
    Auto,
    Chinese,
    English,
}

internal static class OverlayText
{
    internal static string PanelTitle => Pick("Damage Tracker", "伤害统计");
    internal static string CollapseTooltip => Pick("Collapse / expand", "收起/展开");
    internal static string CurrentCombatTitle => Pick("Current Combat", "当前战斗");
    internal static string RunTotalTitle => Pick("Run Total", "本局累计");
    internal static string LastCombatTitle => Pick("Last Combat", "上一场结算");
    internal static string FooterHotkeys => Pick("F7 Toggle · F8 Demo · F9 Reset", "F7 显示 · F8 测试 · F9 重置");
    internal static string HistoryHint => Pick("Browse earlier fights from this run", "查看本局内更早战斗");
    internal static string HistoryBack => Pick("‹ Back", "‹ 返回");
    internal static string HistoryBackTooltip => Pick("Return to main panel", "返回主面板");
    internal static string HistoryTitle => Pick("Run Combat History", "本局战斗记录");
    internal static string HistoryPrevPage => Pick("‹ Prev", "‹ 上一页");
    internal static string HistoryNextPage => Pick("Next ›", "下一页 ›");
    internal static string HistoryTooltip => Pick("View earlier fights from this run", "查看本局更早战斗记录");
    internal static string ExpandRecordTooltip => Pick("Expand for more players", "展开查看更多玩家");
    internal static string CollapseRecordTooltip => Pick("Collapse combat details", "收起本场详情");

    internal static string HistoryOpenButton(bool historyOpen)
        => historyOpen ? $"{LastCombatTitle} ◂" : $"{LastCombatTitle} ▸";

    internal static string HistoryPage(int currentPage, int totalPages)
        => Pick($"Page {currentPage}/{totalPages}", $"第 {currentPage}/{totalPages} 页");

    internal static string HistoryEmpty()
        => Pick("No earlier combat records to show yet.", "还没有可查看的更早战斗记录。");

    internal static string CurrentCombatEmpty()
        => Pick("No valid damage in this combat yet.", "本场还没有有效伤害。");

    internal static string RunTotalEmpty()
        => Pick("No run damage recorded yet.", "还没有累计伤害。");

    internal static string LastCombatEmpty()
        => Pick("No previous combat summary yet.", "还没有上一场结算。");

    internal static string EncounterInProgressNoDamage()
        => Pick("Combat in progress, but no valid damage yet.", "战斗进行中，暂时还没有有效伤害。");

    internal static string EncounterInProgress(int roundCount, int activeDealers, int totalPlayers, float totalDamage)
        => Pick(
            $"Combat in progress · Round {roundCount} · Active dealers {activeDealers}/{totalPlayers} · Total {totalDamage:F0}",
            $"战斗进行中 · 第 {roundCount} 回合 · 出伤 {activeDealers}/{totalPlayers} 人 · 总伤害 {totalDamage:F0}");

    internal static string EncounterFinishedNoData(bool sawCombat)
        => sawCombat
            ? Pick("Combat finished, but no summary data is ready yet.", "本场已结束，但还没有可展示的结算数据。")
            : Pick("No settled combat yet. Start a fight to begin tracking.", "还没有已结算战斗，开打后会自动开始统计。");

    internal static string EncounterFinished(int roundCount, int activeDealers, int totalPlayers, float totalDamage)
        => Pick(
            $"Combat settled · {roundCount} rounds · Active dealers {activeDealers}/{totalPlayers} · Total {totalDamage:F0}",
            $"本场结算 · {roundCount} 回合 · 出伤 {activeDealers}/{totalPlayers} 人 · 总伤害 {totalDamage:F0}");

    internal static string LifetimeNoDamage()
        => Pick("No valid damage recorded for this run yet.", "当前这一局还没有累计到有效伤害。");

    internal static string LifetimeTotal(float totalDamage)
        => Pick($"Current run total damage {totalDamage:F0}", $"当前这一局累计总伤害 {totalDamage:F0}");

    internal static string LastCombatNoSummary()
        => Pick("No previous combat summary to show yet.", "还没有上一场可展示的结算。");

    internal static string LastCombatSummary(float totalDamage, int activeDealers)
        => Pick($"Last combat total {totalDamage:F0} · Active dealers {activeDealers}", $"上一场总伤害 {totalDamage:F0} · 出伤 {activeDealers} 人");

    internal static string CombatHistoryNoRecords()
        => Pick("No earlier combat records yet.", "还没有更早的战斗记录。");

    internal static string CombatHistorySummary(int recordCount)
        => Pick($"Stored {recordCount} earlier fights from this run", $"本局已保留 {recordCount} 场更早战斗记录");

    internal static string CurrentChampion(DpsTracker.PlayerSnapshot snapshot)
        => Pick(
            $"🏆 Current leader {snapshot.DisplayName} · {snapshot.TotalDamage:F0} · {snapshot.DamagePerTurn:F1} DPT · Best hit {snapshot.HighestSingleHit:F0}",
            $"🏆 当前冠军 {snapshot.DisplayName} · {snapshot.TotalDamage:F0} · {snapshot.DamagePerTurn:F1} DPT · 最高单次 {snapshot.HighestSingleHit:F0}");

    internal static string CombatRecordTitle(int combatIndex)
        => Pick($"Fight {combatIndex}", $"第 {combatIndex} 场");

    internal static string CombatRecordSummary(int roundCount, int activeDealers, float highestSingleHit)
        => Pick(
            $"{roundCount} rounds · Active dealers {activeDealers} · Best hit {highestSingleHit:F0}",
            $"{roundCount} 回合 · 出伤 {activeDealers} 人 · 最高单次 {highestSingleHit:F0}");

    internal static string CombatRecordChampion(DpsTracker.PlayerSnapshot snapshot)
        => Pick(
            $"🏆 Leader {snapshot.DisplayName} · {snapshot.TotalDamage:F0} · Best hit {snapshot.HighestSingleHit:F0}",
            $"🏆 冠军 {snapshot.DisplayName} · {snapshot.TotalDamage:F0} · 最高单次 {snapshot.HighestSingleHit:F0}");

    internal static string CombatRecordPlayer(DpsTracker.PlayerSnapshot snapshot, bool isChampion)
    {
        string crown = isChampion ? "  👑" : string.Empty;
        return Pick(
            $"• {snapshot.DisplayName}  {snapshot.TotalDamage:F0}  /  {snapshot.DamagePerTurn:F1} DPT  /  Best {snapshot.HighestSingleHit:F0}{crown}",
            $"• {snapshot.DisplayName}  {snapshot.TotalDamage:F0}  /  {snapshot.DamagePerTurn:F1} DPT  /  最高 {snapshot.HighestSingleHit:F0}{crown}");
    }

    internal static string CombatRecordMorePlayers(int hiddenPlayers)
        => Pick($"{hiddenPlayers} more players — expand to view", $"还有 {hiddenPlayers} 位玩家，点击展开查看");

    internal static string RowMetric(DpsTracker.PlayerSnapshot snapshot, bool showDpt)
    {
        if (!showDpt)
            return $"{snapshot.TotalDamage:F0}";

        return snapshot.TotalDamage > 0f ? $"{snapshot.DamagePerTurn:F1} DPT" : Pick("Idle", "待命");
    }

    internal static string RowDetail(DpsTracker.PlayerSnapshot snapshot)
        => Pick($"Total {snapshot.TotalDamage:F0} · Best hit {snapshot.HighestSingleHit:F0}", $"总伤害 {snapshot.TotalDamage:F0} · 最高单次 {snapshot.HighestSingleHit:F0}");

    internal static OverlayLanguageMode CurrentLanguage()
    {
        return PrototypeSettings.OverlayLanguage switch
        {
            OverlayLanguageMode.Chinese => OverlayLanguageMode.Chinese,
            OverlayLanguageMode.English => OverlayLanguageMode.English,
            _ => DetectAutoLanguage(),
        };
    }

    internal static string CurrentLanguageToken()
        => CurrentLanguage() == OverlayLanguageMode.English ? "en" : "zhs";

    private static OverlayLanguageMode DetectAutoLanguage()
    {
        string locale = TranslationServer.GetLocale().ToLowerInvariant();
        return locale.StartsWith("zh") ? OverlayLanguageMode.Chinese : OverlayLanguageMode.English;
    }

    private static string Pick(string en, string zhs)
        => CurrentLanguage() == OverlayLanguageMode.English ? en : zhs;
}
