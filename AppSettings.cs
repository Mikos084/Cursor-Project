using System.Text.Json.Serialization;

namespace MultiplePointers;

internal enum HotkeyAction
{
    ToggleScreens = 1,
    TogglePark = 2,
    StartStop = 3,
    Reset = 4,
    OpenPanel = 5,
    RefreshMonitors = 6,
    SwapMonitorRoles = 7,
    OpenInstructions = 8,
    OpenHotkeySettings = 9
}

internal sealed class HotkeyBinding
{
    public bool Enabled { get; set; } = true;
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Keys Key { get; set; } = Keys.None;

    [JsonIgnore]
    public uint Modifiers
    {
        get
        {
            uint value = NativeMethods.MOD_NOREPEAT;

            if (Ctrl)
                value |= NativeMethods.MOD_CONTROL;

            if (Alt)
                value |= NativeMethods.MOD_ALT;

            if (Shift)
                value |= NativeMethods.MOD_SHIFT;

            return value;
        }
    }

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            if (!Enabled)
                return "Wyłączony";

            if (Key == Keys.None)
                return "Nie ustawiono";

            var parts = new List<string>();

            if (Ctrl)
                parts.Add("Ctrl");

            if (Alt)
                parts.Add("Alt");

            if (Shift)
                parts.Add("Shift");

            parts.Add(PrettyKey(Key));

            return string.Join("+", parts);
        }
    }

    public HotkeyBinding Clone()
        => new()
        {
            Enabled = Enabled,
            Ctrl = Ctrl,
            Alt = Alt,
            Shift = Shift,
            Key = Key
        };

    public string CanonicalKey()
        => $"{Ctrl}:{Alt}:{Shift}:{(int)Key}";

    public static string PrettyKey(Keys key)
    {
        return key switch
        {
            Keys.Space => "Space",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.OemMinus => "-",
            Keys.Oemplus => "+",
            Keys.Return => "Enter",
            Keys.Escape => "Esc",
            Keys.Prior => "PageUp",
            Keys.Next => "PageDown",
            _ => key.ToString()
        };
    }
}

internal sealed class AppSettings
{
    public HotkeyBinding ToggleScreens { get; set; } =
        New(true, false, true, Keys.F7);

    public HotkeyBinding TogglePark { get; set; } =
        New(true, false, true, Keys.F8);

    public HotkeyBinding StartStop { get; set; } =
        New(true, false, true, Keys.F9);

    public HotkeyBinding Reset { get; set; } =
        New(true, false, true, Keys.F10);

    public HotkeyBinding OpenPanel { get; set; } =
        New(true, false, true, Keys.F11);

    public HotkeyBinding RefreshMonitors { get; set; } =
        Disabled(Keys.F12);

    public HotkeyBinding SwapMonitorRoles { get; set; } =
        Disabled(Keys.F6);

    public HotkeyBinding OpenInstructions { get; set; } =
        Disabled(Keys.F5);

    public HotkeyBinding OpenHotkeySettings { get; set; } =
        Disabled(Keys.F4);

    public HotkeyBinding Get(HotkeyAction action)
        => action switch
        {
            HotkeyAction.ToggleScreens => ToggleScreens,
            HotkeyAction.TogglePark => TogglePark,
            HotkeyAction.StartStop => StartStop,
            HotkeyAction.Reset => Reset,
            HotkeyAction.OpenPanel => OpenPanel,
            HotkeyAction.RefreshMonitors => RefreshMonitors,
            HotkeyAction.SwapMonitorRoles => SwapMonitorRoles,
            HotkeyAction.OpenInstructions => OpenInstructions,
            HotkeyAction.OpenHotkeySettings => OpenHotkeySettings,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

    public IEnumerable<(HotkeyAction Action, HotkeyBinding Binding)> Enumerate()
    {
        yield return (HotkeyAction.ToggleScreens, ToggleScreens);
        yield return (HotkeyAction.TogglePark, TogglePark);
        yield return (HotkeyAction.StartStop, StartStop);
        yield return (HotkeyAction.Reset, Reset);
        yield return (HotkeyAction.OpenPanel, OpenPanel);
        yield return (HotkeyAction.RefreshMonitors, RefreshMonitors);
        yield return (HotkeyAction.SwapMonitorRoles, SwapMonitorRoles);
        yield return (HotkeyAction.OpenInstructions, OpenInstructions);
        yield return (HotkeyAction.OpenHotkeySettings, OpenHotkeySettings);
    }

    public AppSettings Clone()
        => new()
        {
            ToggleScreens = ToggleScreens.Clone(),
            TogglePark = TogglePark.Clone(),
            StartStop = StartStop.Clone(),
            Reset = Reset.Clone(),
            OpenPanel = OpenPanel.Clone(),
            RefreshMonitors = RefreshMonitors.Clone(),
            SwapMonitorRoles = SwapMonitorRoles.Clone(),
            OpenInstructions = OpenInstructions.Clone(),
            OpenHotkeySettings = OpenHotkeySettings.Clone()
        };

    public static AppSettings Defaults()
        => new();

    public static string ActionName(HotkeyAction action)
        => action switch
        {
            HotkeyAction.ToggleScreens => "Przełącz prezentacja ↔ prywatny",
            HotkeyAction.TogglePark => "Parkuj / usuń strzałkę",
            HotkeyAction.StartStop => "Start / stop prezentacji",
            HotkeyAction.Reset => "Reset / odblokuj",
            HotkeyAction.OpenPanel => "Otwórz panel",
            HotkeyAction.RefreshMonitors => "Odśwież monitory",
            HotkeyAction.SwapMonitorRoles => "Zamień role monitorów",
            HotkeyAction.OpenInstructions => "Otwórz instrukcję",
            HotkeyAction.OpenHotkeySettings => "Otwórz ustawienia skrótów",
            _ => action.ToString()
        };

    private static HotkeyBinding New(
        bool ctrl,
        bool alt,
        bool shift,
        Keys key)
        => new()
        {
            Enabled = true,
            Ctrl = ctrl,
            Alt = alt,
            Shift = shift,
            Key = key
        };

    private static HotkeyBinding Disabled(Keys suggestedKey)
        => new()
        {
            Enabled = false,
            Ctrl = true,
            Alt = false,
            Shift = true,
            Key = suggestedKey
        };
}
