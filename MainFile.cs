using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2DpsPrototype;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Godot.Node
{
    internal const string ModId = "sts2.jason.dpsprototype";
    internal const string Version = "1.0.2";
    internal static readonly Logger Log = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        PrototypeSettings.Load();
        DamageHookPatches.EnsurePatched();
        DamageDebugPatches.EnsurePatched();
        PrototypeController.EnsureBootstrapped();

        Log.Info($"DPS Prototype v{Version} initialized.");
    }
}

