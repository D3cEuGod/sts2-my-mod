namespace Sts2DpsPrototype;

internal static class PrototypeSettings
{
    internal static bool ShowPanel { get; private set; } = true;
    internal static float PanelOpacity { get; private set; } = 0.78f;
    internal static int MaxRows { get; private set; } = 5;
    internal static bool EnableDemoHotkeys { get; private set; } = true;
    internal static OverlayLanguageMode OverlayLanguage { get; private set; } = OverlayLanguageMode.Auto;

    internal static void Load()
    {
        ShowPanel = ModConfigBridge.GetValue("showPanel", true);
        PanelOpacity = Math.Clamp(ModConfigBridge.GetValue("panelOpacity", 0.78f), 0.35f, 0.95f);
        MaxRows = Math.Clamp(ModConfigBridge.GetValue("maxRows", 5), 3, 8);
        EnableDemoHotkeys = ModConfigBridge.GetValue("enableDemoHotkeys", true);
        SetOverlayLanguage(ModConfigBridge.GetValue("overlayLanguage", "auto"));
    }

    internal static void SetShowPanel(bool value) => ShowPanel = value;
    internal static void SetPanelOpacity(float value) => PanelOpacity = Math.Clamp(value, 0.35f, 0.95f);
    internal static void SetMaxRows(int value) => MaxRows = Math.Clamp(value, 3, 8);
    internal static void SetEnableDemoHotkeys(bool value) => EnableDemoHotkeys = value;

    internal static void SetOverlayLanguage(string? value)
    {
        OverlayLanguage = value?.Trim().ToLowerInvariant() switch
        {
            "en" or "english" => OverlayLanguageMode.English,
            "zh" or "zhs" or "chinese" or "中文" or "简体中文" => OverlayLanguageMode.Chinese,
            _ => OverlayLanguageMode.Auto,
        };
    }
}
