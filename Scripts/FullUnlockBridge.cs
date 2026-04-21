using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace Sts2DpsPrototype;

internal static class FullUnlockBridge
{
    private static bool _applied;

    internal static void TryApply()
    {
        if (_applied)
            return;

        MainFile.Log.Info("FullUnlockBridge disabled for now to avoid corrupting timeline epoch data.");
        _applied = true;
    }

    private static bool ForceUnlockProgressFile(SaveManager saveManager)
    {
        string moddedProgressPath;
        try
        {
            moddedProgressPath = saveManager.GetProfileScopedPath("saves/progress.save");
        }
        catch
        {
            return false;
        }

        if (!File.Exists(moddedProgressPath))
            return false;

        string baseProgressPath = moddedProgressPath.Replace(
            Path.DirectorySeparatorChar + "modded" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar.ToString());

        JsonObject modded = JsonNode.Parse(File.ReadAllText(moddedProgressPath))?.AsObject() ?? [];
        JsonObject? baseJson = File.Exists(baseProgressPath)
            ? JsonNode.Parse(File.ReadAllText(baseProgressPath))?.AsObject()
            : null;

        bool changed = false;

        changed |= MergeUniqueArray(modded, baseJson, "discovered_acts");
        changed |= MergeUniqueArray(modded, baseJson, "discovered_cards");
        changed |= MergeUniqueArray(modded, baseJson, "discovered_events");
        changed |= MergeUniqueArray(modded, baseJson, "discovered_potions");
        changed |= MergeUniqueArray(modded, baseJson, "discovered_relics");
        changed |= MergeUniqueArray(modded, baseJson, "ftue_completed");
        changed |= MergeUniqueArray(modded, baseJson, "unlocked_achievements");

        changed |= EnsureAllActsUnlocked(modded);
        changed |= EnsureAllCharactersPresent(modded, baseJson);
        changed |= EnsureAllEpochsUnlocked(modded, baseJson);
        changed |= EnsurePendingCharacterUnlockCleared(modded);

        if (!changed)
            return false;

        string json = JsonSerializer.Serialize(modded, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        File.WriteAllText(moddedProgressPath, json);
        MainFile.Log.Info($"Force-updated progress.save unlock state: {moddedProgressPath}");
        return true;
    }

    private static bool EnsureAllActsUnlocked(JsonObject progress)
    {
        JsonArray acts = progress["discovered_acts"] as JsonArray ?? new JsonArray();
        var seen = acts.Select(x => x?.GetValue<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        bool changed = false;

        foreach (var act in ModelDb.Acts)
        {
            if (act?.Id == null)
                continue;

            string actId = act.Id.ToString();
            if (seen.Add(actId))
            {
                acts.Add(actId);
                changed = true;
            }
        }

        progress["discovered_acts"] = acts;
        return changed;
    }

    private static bool EnsureAllCharactersPresent(JsonObject progress, JsonObject? baseJson)
    {
        JsonArray result = MergeById(progress["character_stats"] as JsonArray, baseJson?["character_stats"] as JsonArray, "id");
        var seen = result.OfType<JsonObject>()
            .Select(x => x["id"]?.GetValue<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        bool changed = (progress["character_stats"] as JsonArray)?.Count != result.Count;

        foreach (var character in ModelDb.AllCharacters)
        {
            if (character?.Id == null)
                continue;

            string characterId = character.Id.ToString();
            if (!seen.Add(characterId))
                continue;

            JsonObject? fromBase = baseJson?["character_stats"] is JsonArray baseStats
                ? baseStats.OfType<JsonObject>().FirstOrDefault(x => x["id"]?.GetValue<string>() == characterId)?.DeepClone()?.AsObject()
                : null;

            result.Add(fromBase ?? new JsonObject
            {
                ["id"] = characterId,
                ["wins"] = 0,
                ["losses"] = 0,
                ["highest_ascension"] = 0,
            });
            changed = true;
        }

        progress["character_stats"] = new JsonArray(result.OfType<JsonObject>().OrderBy(x => x["id"]!.GetValue<string>()).Select(x => (JsonNode)x).ToArray());
        return changed;
    }

    private static bool EnsureAllEpochsUnlocked(JsonObject progress, JsonObject? baseJson)
    {
        JsonArray result = MergeById(progress["epochs"] as JsonArray, baseJson?["epochs"] as JsonArray, "id");
        bool changed = (progress["epochs"] as JsonArray)?.Count != result.Count;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (JsonObject epoch in result.OfType<JsonObject>())
        {
            string state = epoch["state"]?.GetValue<string>() ?? "not_obtained";
            long obtainDate = epoch["obtain_date"]?.GetValue<long>() ?? 0;
            if (state != "obtained")
            {
                epoch["state"] = "obtained";
                changed = true;
            }

            if (obtainDate <= 0)
            {
                epoch["obtain_date"] = now;
                changed = true;
            }
        }

        progress["epochs"] = result;
        return changed;
    }

    private static bool EnsurePendingCharacterUnlockCleared(JsonObject progress)
    {
        string current = progress["pending_character_unlock"]?.GetValue<string>() ?? string.Empty;
        if (current == "NONE.NONE")
            return false;

        progress["pending_character_unlock"] = "NONE.NONE";
        return true;
    }

    private static bool MergeUniqueArray(JsonObject target, JsonObject? source, string fieldName)
    {
        JsonArray result = new();
        HashSet<string> seen = [];
        int beforeCount = (target[fieldName] as JsonArray)?.Count ?? 0;

        foreach (JsonArray? array in new JsonArray?[] { target[fieldName] as JsonArray, source?[fieldName] as JsonArray })
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
        return result.Count != beforeCount;
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
}
