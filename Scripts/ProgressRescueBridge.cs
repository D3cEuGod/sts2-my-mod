using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace Sts2DpsPrototype;

internal static class ProgressRescueBridge
{
    private static bool _applied;

    internal static void TryApply()
    {
        if (_applied)
            return;

        SaveManager? saveManager = SaveManager.Instance;
        if (saveManager == null)
            return;

        string moddedProgressPath;
        try
        {
            moddedProgressPath = saveManager.GetProfileScopedPath("saves/progress.save");
        }
        catch
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(moddedProgressPath) || !File.Exists(moddedProgressPath))
        {
            _applied = true;
            return;
        }

        string baseProgressPath = moddedProgressPath.Replace(
            Path.DirectorySeparatorChar + "modded" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar.ToString());

        if (!File.Exists(baseProgressPath))
        {
            _applied = true;
            return;
        }

        JsonObject modded = JsonNode.Parse(File.ReadAllText(moddedProgressPath))?.AsObject() ?? [];
        JsonObject baseJson = JsonNode.Parse(File.ReadAllText(baseProgressPath))?.AsObject() ?? [];

        int moddedEpochCount = (modded["epochs"] as JsonArray)?.Count ?? 0;
        int baseEpochCount = (baseJson["epochs"] as JsonArray)?.Count ?? 0;

        if (moddedEpochCount == 0 && baseEpochCount > 0)
        {
            string backupDir = Path.Combine(Path.GetDirectoryName(moddedProgressPath)!, "..", "manual-fix-backups", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(backupDir);
            File.Copy(moddedProgressPath, Path.Combine(backupDir, "progress.save.before-rescue"), true);
            File.Copy(baseProgressPath, moddedProgressPath, true);
            MainFile.Log.Info($"Rescued empty modded progress.save from base profile: {baseProgressPath} -> {moddedProgressPath}");
        }

        _applied = true;
    }
}
