using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace Sts2DpsPrototype;

internal static class BaseProfileSyncBridge
{
    private static readonly string[] FilesToSync =
    [
        "saves/progress.save",
        "saves/prefs.save",
        "saves/current_run.save",
        "saves/current_run_mp.save",
    ];

    private static readonly string[] DirsToSync =
    [
        "saves/history",
        "replays",
    ];

    private static bool _applied;
    private static readonly List<string> _summary = [];

    internal static void TryApply()
    {
        if (_applied)
            return;

        _summary.Clear();

        SaveManager? saveManager = SaveManager.Instance;
        if (saveManager == null)
            return;

        string? moddedProfileDir = TryGetCurrentProfileDir(saveManager);
        if (string.IsNullOrWhiteSpace(moddedProfileDir) || !moddedProfileDir.Contains(Path.DirectorySeparatorChar + "modded" + Path.DirectorySeparatorChar))
            return;

        string baseProfileDir = moddedProfileDir.Replace(
            Path.DirectorySeparatorChar + "modded" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar.ToString());

        if (!Directory.Exists(baseProfileDir))
        {
            _applied = true;
            return;
        }

        Directory.CreateDirectory(baseProfileDir);
        Directory.CreateDirectory(moddedProfileDir);
        string backupRoot = Path.Combine(moddedProfileDir, "sync-backups", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        bool changed = false;

        foreach (string relativePath in FilesToSync)
            changed |= relativePath == "saves/progress.save"
                ? MergeProgressFileBidirectional(baseProfileDir, moddedProfileDir, relativePath, backupRoot)
                : SyncFileBidirectional(baseProfileDir, moddedProfileDir, relativePath, backupRoot);

        foreach (string relativePath in DirsToSync)
            changed |= SyncDirectoryBidirectional(baseProfileDir, moddedProfileDir, relativePath, backupRoot);

        string summaryPath = Path.Combine(moddedProfileDir, "last-sync-summary.txt");
        if (changed)
        {
            MainFile.Log.Info($"Bidirectionally synced base and modded profiles: {baseProfileDir} <-> {moddedProfileDir}");
            foreach (string line in _summary)
                MainFile.Log.Info($"Sync summary: {line}");

            WriteSummaryFile(summaryPath, baseProfileDir, moddedProfileDir, changed: true);
        }
        else
        {
            MainFile.Log.Info("Base/modded profile sync found no content differences.");
            WriteSummaryFile(summaryPath, baseProfileDir, moddedProfileDir, changed: false);
        }

        _applied = true;
    }

    private static string? TryGetCurrentProfileDir(SaveManager saveManager)
    {
        try
        {
            string progressPath = saveManager.GetProfileScopedPath("saves/progress.save");
            return Path.GetDirectoryName(Path.GetDirectoryName(progressPath));
        }
        catch
        {
            return null;
        }
    }

    private static bool SyncFileBidirectional(string baseProfileDir, string moddedProfileDir, string relativePath, string backupRoot)
    {
        string basePath = Path.Combine(baseProfileDir, relativePath);
        string moddedPath = Path.Combine(moddedProfileDir, relativePath);

        bool baseExists = File.Exists(basePath);
        bool moddedExists = File.Exists(moddedPath);
        if (!baseExists && !moddedExists)
            return false;

        if (baseExists && !moddedExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(moddedPath)!);
            File.Copy(basePath, moddedPath, true);
            _summary.Add($"copied base -> modded: {relativePath}");
            return true;
        }

        if (!baseExists && moddedExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
            File.Copy(moddedPath, basePath, true);
            _summary.Add($"copied modded -> base: {relativePath}");
            return true;
        }

        string canonicalPath = File.GetLastWriteTimeUtc(basePath) >= File.GetLastWriteTimeUtc(moddedPath) ? basePath : moddedPath;
        string canonical = File.ReadAllText(canonicalPath);
        bool wroteBase = WriteIfDifferent(basePath, canonical, backupRoot, relativePath, "base");
        bool wroteModded = WriteIfDifferent(moddedPath, canonical, backupRoot, relativePath, "modded");
        if (wroteBase || wroteModded)
            _summary.Add($"reconciled file {relativePath} (base={wroteBase}, modded={wroteModded})");
        return wroteBase || wroteModded;
    }

    private static bool MergeProgressFileBidirectional(string baseProfileDir, string moddedProfileDir, string relativePath, string backupRoot)
    {
        string basePath = Path.Combine(baseProfileDir, relativePath);
        string moddedPath = Path.Combine(moddedProfileDir, relativePath);

        bool baseExists = File.Exists(basePath);
        bool moddedExists = File.Exists(moddedPath);
        if (!baseExists && !moddedExists)
            return false;

        if (baseExists && !moddedExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(moddedPath)!);
            File.Copy(basePath, moddedPath, true);
            _summary.Add($"copied base -> modded: {relativePath}");
            return true;
        }

        if (!baseExists && moddedExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
            File.Copy(moddedPath, basePath, true);
            _summary.Add($"copied modded -> base: {relativePath}");
            return true;
        }

        JsonObject baseJson = JsonNode.Parse(File.ReadAllText(basePath))!.AsObject();
        JsonObject moddedJson = JsonNode.Parse(File.ReadAllText(moddedPath))!.AsObject();
        JsonObject merged = baseJson.DeepClone().AsObject();
        List<string> progressChanges = [];

        MergeScalarMax(merged, moddedJson, "architect_damage", progressChanges);
        MergeScalarMax(merged, moddedJson, "current_score", progressChanges);
        MergeScalarMax(merged, moddedJson, "floors_climbed", progressChanges);
        MergeScalarMax(merged, moddedJson, "max_multiplayer_ascension", progressChanges);
        MergeScalarMax(merged, moddedJson, "preferred_multiplayer_ascension", progressChanges);
        MergeScalarMax(merged, moddedJson, "schema_version", progressChanges);
        MergeScalarMax(merged, moddedJson, "test_subject_kills", progressChanges);
        MergeScalarMax(merged, moddedJson, "total_playtime", progressChanges);
        MergeScalarMax(merged, moddedJson, "total_unlocks", progressChanges);
        MergeScalarMax(merged, moddedJson, "wongo_points", progressChanges);

        MergeUniqueArray(merged, moddedJson, "discovered_acts", progressChanges);
        MergeUniqueArray(merged, moddedJson, "discovered_cards", progressChanges);
        MergeUniqueArray(merged, moddedJson, "discovered_events", progressChanges);
        MergeUniqueArray(merged, moddedJson, "discovered_potions", progressChanges);
        MergeUniqueArray(merged, moddedJson, "discovered_relics", progressChanges);
        MergeUniqueArray(merged, moddedJson, "ftue_completed", progressChanges);
        MergeUniqueArray(merged, moddedJson, "unlocked_achievements", progressChanges);

        MergeEpochs(merged, moddedJson, progressChanges);
        MergeCharacterStats(merged, moddedJson, progressChanges);
        MergeIdObjects(merged, moddedJson, "ancient_stats", "ancient_id", progressChanges);
        MergeIdObjects(merged, moddedJson, "card_stats", "id", progressChanges);
        MergeIdObjects(merged, moddedJson, "encounter_stats", "id", progressChanges);
        MergeIdObjects(merged, moddedJson, "enemy_stats", "id", progressChanges);

        PreferTruthy(merged, moddedJson, "enable_ftues", progressChanges);
        PreferIfMissing(merged, moddedJson, "unique_id", progressChanges);

        string mergedJson = JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        bool wroteBase = WriteIfDifferent(basePath, mergedJson, backupRoot, relativePath, "base");
        bool wroteModded = WriteIfDifferent(moddedPath, mergedJson, backupRoot, relativePath, "modded");
        if (wroteBase || wroteModded)
        {
            string detail = progressChanges.Count > 0 ? string.Join(", ", progressChanges.Distinct()) : "content normalized";
            _summary.Add($"merged progress.save (base={wroteBase}, modded={wroteModded}): {detail}");
        }
        return wroteBase || wroteModded;
    }

    private static void MergeCharacterStats(JsonObject target, JsonObject source, List<string> summary)
    {
        JsonArray? before = target["character_stats"] as JsonArray;
        JsonArray after = MergeById(before, source["character_stats"] as JsonArray, "id");
        if ((before?.Count ?? 0) != after.Count)
            summary.Add($"character_stats count -> {after.Count}");
        target["character_stats"] = after;
    }

    private static void MergeEpochs(JsonObject target, JsonObject source, List<string> summary)
    {
        JsonArray merged = MergeById(target["epochs"] as JsonArray, source["epochs"] as JsonArray, "id");
        foreach (JsonObject epoch in merged.OfType<JsonObject>())
        {
            if (source["epochs"] is not JsonArray sourceEpochs)
                continue;

            string? id = epoch["id"]?.GetValue<string>();
            JsonObject? sourceEpoch = sourceEpochs.OfType<JsonObject>().FirstOrDefault(x => x["id"]?.GetValue<string>() == id);
            if (sourceEpoch == null)
                continue;

            int targetState = EpochRank(epoch["state"]?.GetValue<string>());
            int sourceState = EpochRank(sourceEpoch["state"]?.GetValue<string>());
            if (sourceState > targetState)
            {
                epoch["state"] = sourceEpoch["state"]?.DeepClone();
                summary.Add($"epoch {id} -> {sourceEpoch["state"]?.GetValue<string>()}");
            }

            long targetDate = epoch["obtain_date"]?.GetValue<long>() ?? 0;
            long sourceDate = sourceEpoch["obtain_date"]?.GetValue<long>() ?? 0;
            if (sourceDate > targetDate)
                epoch["obtain_date"] = sourceDate;
        }

        target["epochs"] = merged;
    }

    private static int EpochRank(string? state) => state switch
    {
        "obtained" => 5,
        "revealed" => 4,
        "obtained_no_slot" => 3,
        "not_obtained" => 2,
        "no_slot" => 1,
        _ => 0,
    };

    private static void MergeIdObjects(JsonObject target, JsonObject source, string fieldName, string idField, List<string> summary)
    {
        JsonArray? before = target[fieldName] as JsonArray;
        JsonArray after = MergeById(before, source[fieldName] as JsonArray, idField);
        if ((before?.Count ?? 0) != after.Count)
            summary.Add($"{fieldName} count -> {after.Count}");
        target[fieldName] = after;
    }

    private static JsonArray MergeById(JsonArray? left, JsonArray? right, string idField)
    {
        var merged = new Dictionary<string, JsonObject>();

        void Add(JsonArray? array)
        {
            if (array == null)
                return;

            foreach (JsonObject item in array.OfType<JsonObject>())
            {
                string? id = item[idField]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!merged.TryGetValue(id, out JsonObject? existing))
                {
                    merged[id] = item.DeepClone().AsObject();
                    continue;
                }

                foreach (var pair in item)
                    existing[pair.Key] = ChooseBetter(existing[pair.Key], pair.Value);
            }
        }

        Add(left);
        Add(right);
        return new JsonArray(merged.Values.OrderBy(x => x[idField]!.GetValue<string>()).Select(x => (JsonNode)x).ToArray());
    }

    private static JsonNode? ChooseBetter(JsonNode? current, JsonNode? incoming)
    {
        if (current == null)
            return incoming?.DeepClone();
        if (incoming == null)
            return current;

        if (current is JsonValue currentValue && incoming is JsonValue incomingValue)
        {
            if (TryGetLong(currentValue, out long currentLong) && TryGetLong(incomingValue, out long incomingLong))
                return incomingLong > currentLong ? incoming.DeepClone() : current;
        }

        if (current is JsonArray currentArray && incoming is JsonArray incomingArray)
        {
            var values = currentArray.Select(x => x?.ToJsonString()).ToHashSet();
            var result = new JsonArray(currentArray.Select(x => x?.DeepClone()).ToArray());
            foreach (JsonNode? node in incomingArray)
            {
                string? key = node?.ToJsonString();
                if (key != null && values.Add(key))
                    result.Add(node?.DeepClone());
            }
            return result;
        }

        return current;
    }

    private static void MergeScalarMax(JsonObject target, JsonObject source, string fieldName, List<string> summary)
    {
        if (target[fieldName] is not JsonValue targetValue || source[fieldName] is not JsonValue sourceValue)
            return;

        if (!TryGetLong(targetValue, out long targetLong) || !TryGetLong(sourceValue, out long sourceLong))
            return;

        if (sourceLong > targetLong)
        {
            target[fieldName] = source[fieldName]!.DeepClone();
            summary.Add($"{fieldName} -> {sourceLong}");
        }
    }

    private static bool TryGetLong(JsonValue value, out long result)
    {
        if (value.TryGetValue(out int intValue))
        {
            result = intValue;
            return true;
        }
        if (value.TryGetValue(out long longValue))
        {
            result = longValue;
            return true;
        }
        result = 0;
        return false;
    }

    private static void MergeUniqueArray(JsonObject target, JsonObject source, string fieldName, List<string> summary)
    {
        JsonArray result = new();
        HashSet<string> seen = [];
        int beforeCount = (target[fieldName] as JsonArray)?.Count ?? 0;

        foreach (JsonArray? array in new JsonArray?[] { target[fieldName] as JsonArray, source[fieldName] as JsonArray })
        {
            if (array == null)
                continue;

            foreach (JsonNode? node in array)
            {
                string? key = node?.ToJsonString();
                if (key == null || !seen.Add(key))
                    continue;

                result.Add(node?.DeepClone());
            }
        }

        target[fieldName] = result;
        if (result.Count > beforeCount)
            summary.Add($"{fieldName} count -> {result.Count}");
    }

    private static void PreferTruthy(JsonObject target, JsonObject source, string fieldName, List<string> summary)
    {
        if (target[fieldName] is JsonValue targetValue && source[fieldName] is JsonValue sourceValue &&
            targetValue.TryGetValue(out bool targetBool) && sourceValue.TryGetValue(out bool sourceBool) &&
            sourceBool && !targetBool)
        {
            target[fieldName] = true;
            summary.Add($"{fieldName} -> true");
        }
    }

    private static void PreferIfMissing(JsonObject target, JsonObject source, string fieldName, List<string> summary)
    {
        if (target[fieldName] == null || string.IsNullOrWhiteSpace(target[fieldName]?.GetValue<string>()))
        {
            target[fieldName] = source[fieldName]?.DeepClone();
            summary.Add($"{fieldName} copied");
        }
    }

    private static bool SyncDirectoryBidirectional(string baseProfileDir, string moddedProfileDir, string relativePath, string backupRoot)
    {
        string baseDir = Path.Combine(baseProfileDir, relativePath);
        string moddedDir = Path.Combine(moddedProfileDir, relativePath);

        var relPaths = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(baseDir))
        {
            foreach (string path in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
                relPaths.Add(Path.GetRelativePath(baseProfileDir, path));
        }

        if (Directory.Exists(moddedDir))
        {
            foreach (string path in Directory.EnumerateFiles(moddedDir, "*", SearchOption.AllDirectories))
                relPaths.Add(Path.GetRelativePath(moddedProfileDir, path));
        }

        bool changed = false;
        foreach (string childRelative in relPaths)
            changed |= SyncFileBidirectional(baseProfileDir, moddedProfileDir, childRelative, backupRoot);

        return changed;
    }

    private static bool WriteIfDifferent(string path, string newContent, string backupRoot, string relativePath, string side)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path))
        {
            string current = File.ReadAllText(path);
            if (current == newContent)
                return false;

            BackupIfExists(path, backupRoot, relativePath);
        }

        File.WriteAllText(path, newContent);
        return true;
    }

    private static void BackupIfExists(string destPath, string backupRoot, string relativePath)
    {
        if (!File.Exists(destPath))
            return;

        string side = destPath.Contains(Path.DirectorySeparatorChar + "modded" + Path.DirectorySeparatorChar)
            ? "modded"
            : "base";
        string backupPath = Path.Combine(backupRoot, side, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        File.Copy(destPath, backupPath, true);
    }

    private static void WriteSummaryFile(string summaryPath, string baseProfileDir, string moddedProfileDir, bool changed)
    {
        List<string> lines =
        [
            $"timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}",
            $"base_profile: {baseProfileDir}",
            $"modded_profile: {moddedProfileDir}",
            $"changed: {changed}",
        ];

        if (_summary.Count == 0)
            lines.Add("summary: no content differences");
        else
            lines.AddRange(_summary.Select(line => $"- {line}"));

        File.WriteAllText(summaryPath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }
}
