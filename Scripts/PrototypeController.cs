using Godot;

namespace Sts2DpsPrototype;

internal sealed partial class PrototypeController : Node
{
    private const string ControllerName = "Sts2DpsPrototypeController";

    private readonly CombatRuntimeBridge _combatBridge = new();
    private bool _togglePressedLastFrame;
    private bool _simulatePressedLastFrame;
    private bool _resetPressedLastFrame;

    internal static void EnsureBootstrapped()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        tree.ProcessFrame += AddOnNextFrame;
    }

    private static void AddOnNextFrame()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        tree.ProcessFrame -= AddOnNextFrame;

        if (tree.Root.GetNodeOrNull<Node>(ControllerName) != null)
            return;

        var controller = new PrototypeController
        {
            Name = ControllerName,
            ProcessMode = ProcessModeEnum.Always,
        };

        tree.Root.AddChild(controller);
    }

    public override void _Ready()
    {
        MainFile.Log.Info("PrototypeController ready, creating DpsOverlay.");
        AddChild(new DpsOverlay());
    }

    public override void _Process(double delta)
    {
        ProgressRescueBridge.TryApply();
        BaseProfileSyncBridge.TryApply();
        FullUnlockBridge.TryApply();
        NeowProgressBridge.Tick();
        AscensionUnlocker.TryApply();
        _combatBridge.Update();
        DpsTracker.Tick(delta);
        HandleDemoHotkeys();
    }

    public override void _ExitTree()
    {
        _combatBridge.Dispose();
    }

    private void HandleDemoHotkeys()
    {
        if (!PrototypeSettings.EnableDemoHotkeys)
            return;

        bool togglePressed = Input.IsKeyPressed(Key.F7);
        bool simulatePressed = Input.IsKeyPressed(Key.F8);
        bool resetPressed = Input.IsKeyPressed(Key.F9);

        if (togglePressed && !_togglePressedLastFrame)
            PrototypeSettings.SetShowPanel(!PrototypeSettings.ShowPanel);

        if (simulatePressed && !_simulatePressedLastFrame)
            SimulateBurst();

        if (resetPressed && !_resetPressedLastFrame)
            _combatBridge.ResetTrackedCombat();

        _togglePressedLastFrame = togglePressed;
        _simulatePressedLastFrame = simulatePressed;
        _resetPressedLastFrame = resetPressed;
    }

    private void SimulateBurst()
    {
        DamageEventBridge.ResetCombat();
        EmitFixedDamage("demo-player", "Ironclad", 200f);
    }

    private static void EmitFixedDamage(string playerId, string playerName, float damage)
    {
        DamageEventBridge.RecordDamage(playerId, playerName, damage);
    }

}
