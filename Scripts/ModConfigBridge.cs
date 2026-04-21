using System.Reflection;
using Godot;

namespace Sts2DpsPrototype;

internal static class ModConfigBridge
{
    private static bool _available;
    private static bool _registered;
    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    internal static void DeferredRegister()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        tree.ProcessFrame += OnNextFrame;
    }

    internal static T GetValue<T>(string key, T fallback)
    {
        if (!_available || _apiType == null)
            return fallback;

        try
        {
            object? result = _apiType.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(typeof(T))
                .Invoke(null, new object[] { MainFile.ModId, key });

            return result is T typed ? typed : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void OnNextFrame()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        tree.ProcessFrame -= OnNextFrame;
        Detect();
        if (_available)
            Register();
        PrototypeSettings.Load();
    }

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .ToArray();

            _apiType = allTypes.FirstOrDefault(type => type.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(type => type.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(type => type.FullName == "ModConfig.ConfigType");
            _available = _apiType != null && _entryType != null && _configTypeEnum != null;
        }
        catch
        {
            _available = false;
        }
    }

    private static void Register()
    {
        if (_registered || _apiType == null || _entryType == null)
            return;

        _registered = true;

        try
        {
            Array entries = BuildEntries();
            var displayNames = new Dictionary<string, string>
            {
                ["en"] = "DPS Prototype",
                ["zhs"] = "DPS 原型",
            };

            MethodInfo registerMethod = _apiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "Register")
                .OrderByDescending(method => method.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(null, new object[]
                {
                    MainFile.ModId,
                    displayNames["en"],
                    displayNames,
                    entries
                });
            }
            else
            {
                registerMethod.Invoke(null, new object[]
                {
                    MainFile.ModId,
                    displayNames["en"],
                    entries
                });
            }
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[DpsPrototype] ModConfig registration failed: {exception}");
        }
    }

    private static Array BuildEntries()
    {
        var entries = new List<object>
        {
            Entry(config =>
            {
                Set(config, "Label", "Overlay");
                Set(config, "Labels", L("Overlay", "统计面板"));
                Set(config, "Type", EnumVal("Header"));
            }),
            Entry(config =>
            {
                Set(config, "Key", "showPanel");
                Set(config, "Label", "Show panel");
                Set(config, "Labels", L("Show panel", "显示面板"));
                Set(config, "Type", EnumVal("Toggle"));
                Set(config, "DefaultValue", true);
                Set(config, "OnChanged", new Action<object>(value => PrototypeSettings.SetShowPanel(Convert.ToBoolean(value))));
            }),
            Entry(config =>
            {
                Set(config, "Key", "panelOpacity");
                Set(config, "Label", "Opacity");
                Set(config, "Labels", L("Opacity", "透明度"));
                Set(config, "Type", EnumVal("Slider"));
                Set(config, "DefaultValue", 0.78f);
                Set(config, "Min", 0.35f);
                Set(config, "Max", 0.95f);
                Set(config, "Step", 0.05f);
                Set(config, "Format", "F2");
                Set(config, "OnChanged", new Action<object>(value => PrototypeSettings.SetPanelOpacity(Convert.ToSingle(value))));
            }),
            Entry(config =>
            {
                Set(config, "Key", "maxRows");
                Set(config, "Label", "Visible rows");
                Set(config, "Labels", L("Visible rows", "显示行数"));
                Set(config, "Type", EnumVal("Slider"));
                Set(config, "DefaultValue", 5f);
                Set(config, "Min", 3f);
                Set(config, "Max", 8f);
                Set(config, "Step", 1f);
                Set(config, "Format", "F0");
                Set(config, "OnChanged", new Action<object>(value => PrototypeSettings.SetMaxRows((int)Convert.ToSingle(value))));
            }),
            Entry(config =>
            {
                Set(config, "Key", "enableDemoHotkeys");
                Set(config, "Label", "Enable demo hotkeys");
                Set(config, "Labels", L("Enable demo hotkeys", "启用演示热键"));
                Set(config, "Type", EnumVal("Toggle"));
                Set(config, "DefaultValue", true);
                Set(config, "OnChanged", new Action<object>(value => PrototypeSettings.SetEnableDemoHotkeys(Convert.ToBoolean(value))));
            })
        };

        var typedArray = Array.CreateInstance(_entryType!, entries.Count);
        for (int index = 0; index < entries.Count; index++)
            typedArray.SetValue(entries[index], index);
        return typedArray;
    }

    private static object Entry(Action<object> configure)
    {
        object instance = Activator.CreateInstance(_entryType!)!;
        configure(instance);
        return instance;
    }

    private static void Set(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName)?.SetValue(target, value);
    }

    private static Dictionary<string, string> L(string en, string zhs)
    {
        return new Dictionary<string, string>
        {
            ["en"] = en,
            ["zhs"] = zhs,
        };
    }

    private static object EnumVal(string name)
    {
        return Enum.Parse(_configTypeEnum!, name);
    }
}
