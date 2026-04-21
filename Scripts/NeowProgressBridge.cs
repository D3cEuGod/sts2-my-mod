using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Sts2DpsPrototype;

internal static class NeowProgressBridge
{
    private const string NeowEpochId = "NEOW_EPOCH";

    private static bool _progressChecked;
    private static bool _runFlagChecked;

    internal static void Tick()
    {
        RestoreProgressIfNeeded();
        RestoreRunFlagIfNeeded();
    }

    private static void RestoreProgressIfNeeded()
    {
        if (_progressChecked)
            return;

        SaveManager? saveManager = SaveManager.Instance;
        if (saveManager == null)
            return;

        if (!saveManager.IsNeowDiscovered())
            MainFile.Log.Info("Skipping NEOW_EPOCH reveal because this game build rejects RevealEpoch(NEOW_EPOCH).");

        _progressChecked = true;
    }

    private static void RestoreRunFlagIfNeeded()
    {
        if (_runFlagChecked)
            return;

        SaveManager? saveManager = SaveManager.Instance;
        RunManager? runManager = RunManager.Instance;
        if (saveManager == null || runManager == null || !runManager.IsInProgress)
            return;

        if (!saveManager.IsNeowDiscovered())
            return;

        var runState = runManager.DebugOnlyGetState();
        if (runState == null)
            return;

        if (!runState.ExtraFields.StartedWithNeow)
        {
            runState.ExtraFields.StartedWithNeow = true;
            MainFile.Log.Info("Restored StartedWithNeow for the active run.");
        }

        _runFlagChecked = true;
    }
}
