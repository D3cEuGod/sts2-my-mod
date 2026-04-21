using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace Sts2DpsPrototype;

internal static class AscensionUnlocker
{
    private const int TargetAscension = 10;

    private static readonly FieldInfo? CharacterMaxAscensionField = typeof(MegaCrit.Sts2.Core.Saves.CharacterStats)
        .GetField("<MaxAscension>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? CharacterPreferredAscensionField = typeof(MegaCrit.Sts2.Core.Saves.CharacterStats)
        .GetField("<PreferredAscension>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? MaxMultiplayerAscensionField = typeof(ProgressState)
        .GetField("<MaxMultiplayerAscension>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? PreferredMultiplayerAscensionField = typeof(ProgressState)
        .GetField("<PreferredMultiplayerAscension>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool _applied;

    internal static void TryApply()
    {
        if (_applied)
            return;

        SaveManager? saveManager = SaveManager.Instance;
        if (saveManager == null)
            return;

        ProgressState progress = saveManager.Progress;
        bool changed = false;

        foreach (var character in ModelDb.AllCharacters)
        {
            if (character == null)
                continue;

            var stats = progress.GetOrCreateCharacterStats(character.Id);
            changed |= RaiseCharacterAscension(stats);
        }

        changed |= RaiseProgressAscension(progress);

        if (changed)
        {
            saveManager.SaveProgressFile();
            saveManager.SaveProfile();
            MainFile.Log.Info($"Unlocked ascension 0-{TargetAscension} for profile {saveManager.CurrentProfileId}.");
        }
        else
        {
            MainFile.Log.Info($"Ascension 0-{TargetAscension} already unlocked for profile {saveManager.CurrentProfileId}.");
        }

        _applied = true;
    }

    private static bool RaiseCharacterAscension(MegaCrit.Sts2.Core.Saves.CharacterStats stats)
    {
        bool changed = false;

        if (CharacterMaxAscensionField != null)
        {
            int currentMax = (int)(CharacterMaxAscensionField.GetValue(stats) ?? 0);
            if (currentMax < TargetAscension)
            {
                CharacterMaxAscensionField.SetValue(stats, TargetAscension);
                changed = true;
            }
        }

        if (CharacterPreferredAscensionField != null)
        {
            int currentPreferred = (int)(CharacterPreferredAscensionField.GetValue(stats) ?? 0);
            if (currentPreferred > TargetAscension)
            {
                CharacterPreferredAscensionField.SetValue(stats, TargetAscension);
                changed = true;
            }
        }

        return changed;
    }

    private static bool RaiseProgressAscension(ProgressState progress)
    {
        bool changed = false;

        if (MaxMultiplayerAscensionField != null)
        {
            int currentMax = (int)(MaxMultiplayerAscensionField.GetValue(progress) ?? 0);
            if (currentMax < TargetAscension)
            {
                MaxMultiplayerAscensionField.SetValue(progress, TargetAscension);
                changed = true;
            }
        }

        if (PreferredMultiplayerAscensionField != null)
        {
            int currentPreferred = (int)(PreferredMultiplayerAscensionField.GetValue(progress) ?? 0);
            if (currentPreferred > TargetAscension)
            {
                PreferredMultiplayerAscensionField.SetValue(progress, TargetAscension);
                changed = true;
            }
        }

        return changed;
    }
}
