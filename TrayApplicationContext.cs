using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MultiplePointers;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly PointerController _controller;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly NotifyIcon _trayIcon;
    private readonly Control _uiDispatcher;

    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _mainActionItem;
    private readonly ToolStripMenuItem _controlToggleItem;
    private readonly ToolStripMenuItem _parkItem;
    private readonly ToolStripMenuItem _resetItem;
    private readonly ToolStripMenuItem _openPanelItem;
    private readonly ToolStripMenuItem _refreshMonitorsItem;
    private readonly ToolStripMenuItem _swapMonitorRolesItem;
    private readonly ToolStripMenuItem _instructionsItem;
    private readonly ToolStripMenuItem _hotkeySettingsItem;

    private readonly ToolStripMenuItem _exitItem;
    private bool _brandingOpen;
    private DashboardForm? _dashboard;
    private InstructionForm? _instruction;

    private readonly Icon _appIcon;
    private AppSettings _settings;
    private readonly Dictionary<HotkeyAction, bool> _hotkeyStatus = new();

    public TrayApplicationContext()
    {
        NativeMethods.ClipCursor(IntPtr.Zero);

        // Hidden WinForms control used only to marshal system events back to
        // the UI thread safely.
        _uiDispatcher = new Control();
        _uiDispatcher.CreateControl();

        _controller = new PointerController();
        _controller.StateChanged += RefreshTrayStatus;

        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

        _appIcon = LoadAppIcon();
        _settings = SettingsStore.Load();
        AppBranding.Name = _settings.DisplayName;

        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = new Font("Segoe UI", 9.5f),
            MaximumSize = new Size(540, 620),
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.Text,
            Renderer = new DarkMenuRenderer()
        };

        _statusItem = new ToolStripMenuItem("● Gotowy")
        {
            Enabled = false
        };

        _mainActionItem = new ToolStripMenuItem();
        _mainActionItem.Click += (_, _) => TogglePresentation();

        _controlToggleItem = new ToolStripMenuItem();
        _controlToggleItem.Click += (_, _) =>
            RunAction(_controller.TogglePresentationControl);

        _parkItem = new ToolStripMenuItem();
        _parkItem.Click += (_, _) =>
            RunAction(_controller.TogglePark);

        _resetItem = new ToolStripMenuItem();
        _resetItem.Click += (_, _) =>
        {
            _controller.Reset();
            RefreshTrayStatus();
        };

        _openPanelItem = new ToolStripMenuItem();
        _openPanelItem.Click += (_, _) => ShowDashboard();

        _refreshMonitorsItem = new ToolStripMenuItem();
        _refreshMonitorsItem.Click += (_, _) =>
        {
            _controller.RefreshMonitors();
            RefreshTrayStatus();
        };

        _swapMonitorRolesItem = new ToolStripMenuItem();
        _swapMonitorRolesItem.Click += (_, _) => SwapMonitorRoles();

        _instructionsItem = new ToolStripMenuItem();
        _instructionsItem.Click += (_, _) => ShowInstruction();

        _hotkeySettingsItem = new ToolStripMenuItem();
        _hotkeySettingsItem.Click += (_, _) => ShowHotkeySettings();

        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_mainActionItem);
        menu.Items.Add(_controlToggleItem);
        menu.Items.Add(_parkItem);
        menu.Items.Add(_resetItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_openPanelItem);
        menu.Items.Add(_refreshMonitorsItem);
        menu.Items.Add(_swapMonitorRolesItem);
        menu.Items.Add(_hotkeySettingsItem);
        menu.Items.Add(_instructionsItem);
        menu.Items.Add("Easter egg…", null, (_, _) => ShowBranding());

        menu.Items.Add(new ToolStripSeparator());
        _exitItem = new ToolStripMenuItem("Zakończ " + AppBranding.DisplayName, null, (_, _) => ExitApplication());
        menu.Items.Add(_exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = AppBranding.DisplayName,
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowDashboard();
        };

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        string? startupError = RegisterStartupHotkeys();

        RefreshMenuText();
        RefreshTrayStatus();

        _trayIcon.BalloonTipTitle = AppBranding.DisplayName + " 0.8.3 Beta 2";
        _trayIcon.BalloonTipText = startupError is null
            ? "Gotowe. Skróty globalne można teraz zmieniać w Ustawieniach skrótów."
            : startupError;
        _trayIcon.ShowBalloonTip(3000);
    }

    public bool HotkeysOkay
        => _settings.Enumerate()
            .Where(x => x.Binding.Enabled)
            .All(x =>
                _hotkeyStatus.TryGetValue(x.Action, out bool ok) && ok);

    public string HotkeyStatusText
    {
        get
        {
            var lines = new List<string>();

            foreach (var (action, binding) in _settings.Enumerate())
            {
                string mark;
                if (!binding.Enabled)
                    mark = "—";
                else
                    mark = _hotkeyStatus.TryGetValue(action, out bool ok) && ok
                        ? "✓"
                        : "✗";

                lines.Add(
                    $"{mark}  {binding.DisplayText,-18}  {AppSettings.ActionName(action)}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    private string Shortcut(HotkeyAction action)
        => _settings.Get(action).DisplayText;

    private string MenuText(string title, HotkeyAction action)
    {
        HotkeyBinding binding = _settings.Get(action);
        return binding.Enabled
            ? $"{title}    {binding.DisplayText}"
            : $"{title}    (bez skrótu)";
    }

    private string? RegisterStartupHotkeys()
    {
        if (TryRegisterSettings(_settings, out string? error))
            return null;

        UnregisterAllHotkeys();

        AppSettings defaults = AppSettings.Defaults();
        defaults.DisplayName = _settings.DisplayName;

        if (TryRegisterSettings(defaults, out string? defaultError))
        {
            _settings = defaults;
            SettingsStore.Save(_settings, out _);

            return
                "Zapisane skróty były zajęte. Przywrócono skróty domyślne. " +
                "Możesz je zmienić w Ustawieniach skrótów.";
        }

        UnregisterAllHotkeys();
        _settings = defaults;

        return
            "Nie udało się zarejestrować części skrótów. " +
            (defaultError ?? error ?? "Otwórz Ustawienia skrótów.");
    }

    private bool TryRegisterSettings(
        AppSettings settings,
        out string? error)
    {
        error = null;
        _hotkeyStatus.Clear();

        foreach (var (action, binding) in settings.Enumerate())
        {
            if (!binding.Enabled)
                continue;

            if (binding.Key == Keys.None)
            {
                error =
                    $"Brak klawisza dla funkcji: {AppSettings.ActionName(action)}.";
                return false;
            }

            bool ok = NativeMethods.RegisterHotKey(
                _hotkeyWindow.Handle,
                (int)action,
                binding.Modifiers,
                (uint)binding.Key);

            _hotkeyStatus[action] = ok;

            if (!ok)
            {
                int code = Marshal.GetLastWin32Error();
                error =
                    $"Skrót {binding.DisplayText} dla „{AppSettings.ActionName(action)}” " +
                    $"jest zajęty albo niedostępny. Kod Windows: {code}.";
                return false;
            }
        }

        return true;
    }

    private void UnregisterAllHotkeys()
    {
        foreach (HotkeyAction action in Enum.GetValues<HotkeyAction>())
        {
            NativeMethods.UnregisterHotKey(
                _hotkeyWindow.Handle,
                (int)action);
        }

        _hotkeyStatus.Clear();
    }

    private (bool Success, string? Error) TryApplySettings(
        AppSettings candidate)
    {
        candidate.DisplayName = _settings.DisplayName;
        AppSettings previous = _settings.Clone();

        UnregisterAllHotkeys();

        if (!TryRegisterSettings(candidate, out string? registerError))
        {
            UnregisterAllHotkeys();
            TryRegisterSettings(previous, out _);
            _settings = previous;
            RefreshMenuText();
            RefreshTrayStatus();

            return (false, registerError);
        }

        if (!SettingsStore.Save(candidate, out string? saveError))
        {
            UnregisterAllHotkeys();
            TryRegisterSettings(previous, out _);
            _settings = previous;
            RefreshMenuText();
            RefreshTrayStatus();

            return (false, saveError);
        }

        _settings = candidate.Clone();
        RefreshMenuText();
        RefreshTrayStatus();
        _dashboard?.RefreshState();

        return (true, null);
    }

    private void OnHotkeyPressed(int id)
    {
        HotkeyAction action = (HotkeyAction)id;

        switch (action)
        {
            case HotkeyAction.ToggleScreens:
                RunAction(_controller.TogglePresentationControl);
                break;

            case HotkeyAction.TogglePark:
                RunAction(_controller.TogglePark);
                break;

            case HotkeyAction.StartStop:
                TogglePresentation();
                break;

            case HotkeyAction.Reset:
                _controller.Reset();
                RefreshTrayStatus();
                break;

            case HotkeyAction.OpenPanel:
                ShowDashboard();
                break;

            case HotkeyAction.RefreshMonitors:
                _controller.RefreshMonitors();
                RefreshTrayStatus();
                break;

            case HotkeyAction.SwapMonitorRoles:
                SwapMonitorRoles();
                break;

            case HotkeyAction.OpenInstructions:
                ShowInstruction();
                break;

            case HotkeyAction.OpenHotkeySettings:
                ShowHotkeySettings();
                break;
        }
    }

    private delegate bool PointerAction(out string? error);

    private void RunAction(PointerAction action)
    {
        if (!action(out string? error) &&
            !string.IsNullOrWhiteSpace(error))
        {
            _trayIcon.BalloonTipTitle = AppBranding.DisplayName;
            _trayIcon.BalloonTipText =
                error.Length > 250
                    ? error[..250]
                    : error;

            _trayIcon.ShowBalloonTip(3000);
        }

        RefreshTrayStatus();
        _dashboard?.RefreshState();
    }

    private void TogglePresentation()
    {
        if (_controller.IsSessionActive)
        {
            _controller.Reset();
            RefreshTrayStatus();
            return;
        }

        RunAction(_controller.ParkAndGoToControlScreen);
    }

    private void SwapMonitorRoles()
    {
        if (_controller.IsSessionActive)
        {
            _trayIcon.BalloonTipTitle = AppBranding.DisplayName;
            _trayIcon.BalloonTipText =
                "Najpierw zatrzymaj prezentację, a dopiero potem zamień role monitorów.";
            _trayIcon.ShowBalloonTip(2500);
            return;
        }

        string? presentation = _controller.PresentationScreenName;
        string? control = _controller.ControlScreenName;

        if (string.IsNullOrWhiteSpace(presentation) ||
            string.IsNullOrWhiteSpace(control))
        {
            _trayIcon.BalloonTipTitle = AppBranding.DisplayName;
            _trayIcon.BalloonTipText = "Najpierw wybierz oba monitory.";
            _trayIcon.ShowBalloonTip(2500);
            return;
        }

        _controller.SetPresentationScreen(control);
        _controller.SetControlScreen(presentation);

        RefreshTrayStatus();
        _dashboard?.RefreshState();
    }

    private void ShowDashboard()
    {
        if (_dashboard is null || _dashboard.IsDisposed)
        {
            _dashboard = new DashboardForm(
                _controller,
                _appIcon,
                () => HotkeyStatusText,
                Shortcut,
                ShowInstruction,
                ShowHotkeySettings,
                ShowBranding);

            _dashboard.FormClosed += (_, _) => _dashboard = null;
        }

        if (!_dashboard.Visible)
            _dashboard.Show();

        _dashboard.WindowState = FormWindowState.Normal;
        _dashboard.BringToFront();
        _dashboard.Activate();
        _dashboard.RefreshState();
    }

    private void ShowInstruction()
    {
        if (_instruction is null || _instruction.IsDisposed)
        {
            _instruction = new InstructionForm(
                _appIcon,
                Shortcut);

            _instruction.FormClosed += (_, _) => _instruction = null;
        }

        if (!_instruction.Visible)
            _instruction.Show();

        _instruction.WindowState = FormWindowState.Normal;
        _instruction.BringToFront();
        _instruction.Activate();
    }

    private void ShowBranding()
    {
        if (_brandingOpen) return;
        _brandingOpen = true;
        try
        {
            using var form = new BrandingForm(_appIcon, _settings.DisplayName, TryApplyBranding);
            if (_dashboard is not null && !_dashboard.IsDisposed && _dashboard.Visible)
                form.ShowDialog(_dashboard);
            else
                form.ShowDialog();
        }
        finally { _brandingOpen = false; }
    }

    private (bool Success, string? Error) TryApplyBranding(string input)
    {
        if (!AppBranding.TryValidate(input, out var name, out var error))
            return (false, error);
        var candidate = _settings.Clone();
        candidate.DisplayName = name;
        if (!SettingsStore.Save(candidate, out error))
            return (false, error);
        _settings = candidate;
        AppBranding.Name = name;
        _trayIcon.Text = AppBranding.DisplayName;
        _trayIcon.BalloonTipTitle = AppBranding.DisplayName;
        _exitItem.Text = "Zakończ " + AppBranding.DisplayName;
        _dashboard?.ApplyBranding();
        _instruction?.ApplyBranding();
        foreach (var editor in Application.OpenForms.OfType<HotkeySettingsForm>())
            editor.Text = "Skróty globalne — " + AppBranding.DisplayName;
        return (true, null);
    }

    private void ShowHotkeySettings()
    {
        // Critical for real shortcut capture:
        // while the editor is open, Multiple Pointers must NOT own its
        // existing RegisterHotKey combinations, otherwise Windows can fire
        // WM_HOTKEY before the editor receives the key press.
        UnregisterAllHotkeys();

        using var form = new HotkeySettingsForm(
            _appIcon,
            _settings,
            TryApplySettings);

        IWin32Window? owner =
            _dashboard is not null &&
            !_dashboard.IsDisposed &&
            _dashboard.Visible
                ? _dashboard
                : null;

        DialogResult result = owner is null
            ? form.ShowDialog()
            : form.ShowDialog(owner);

        if (result != DialogResult.OK)
        {
            // User cancelled: restore the exact previous set.
            UnregisterAllHotkeys();

            if (!TryRegisterSettings(_settings, out string? restoreError))
            {
                UnregisterAllHotkeys();

                AppSettings fallback = AppSettings.Defaults();
                fallback.DisplayName = _settings.DisplayName;

                if (TryRegisterSettings(fallback, out _))
                {
                    _settings = fallback;
                    SettingsStore.Save(_settings, out _);
                }

                _trayIcon.BalloonTipTitle = AppBranding.DisplayName;
                _trayIcon.BalloonTipText =
                    restoreError is null
                        ? "Poprzedni skrót stał się niedostępny. Przywrócono bezpieczny zestaw domyślny."
                        : restoreError.Length > 220
                            ? restoreError[..220]
                            : restoreError;

                _trayIcon.ShowBalloonTip(3500);
            }
        }

        RefreshMenuText();
        RefreshTrayStatus();
        _dashboard?.RefreshState();
    }

    private void RefreshMenuText()
    {
        _parkItem.Text =
            MenuText("📌 Parkuj / usuń strzałkę", HotkeyAction.TogglePark);

        _resetItem.Text =
            MenuText("↩ Reset / odblokuj", HotkeyAction.Reset);

        _openPanelItem.Text =
            MenuText("Otwórz panel", HotkeyAction.OpenPanel);

        _refreshMonitorsItem.Text =
            MenuText("Odśwież monitory", HotkeyAction.RefreshMonitors);

        _swapMonitorRolesItem.Text =
            MenuText("Zamień role monitorów", HotkeyAction.SwapMonitorRoles);

        _hotkeySettingsItem.Text =
            MenuText("Ustawienia skrótów...", HotkeyAction.OpenHotkeySettings);

        _instructionsItem.Text =
            MenuText("Instrukcja krok po kroku", HotkeyAction.OpenInstructions);

        RefreshTrayStatus();
    }

    private void RefreshTrayStatus()
    {
        if (_controller.IsControllingPresentation)
        {
            _statusItem.Text = "● Sterujesz ekranem prezentacji";
            _mainActionItem.Text =
                MenuText("■ Zatrzymaj prezentację", HotkeyAction.StartStop);
            _controlToggleItem.Text =
                MenuText("↔ Wróć na ekran prywatny", HotkeyAction.ToggleScreens);
        }
        else if (_controller.IsSessionActive)
        {
            _statusItem.Text = "● Prezentacja aktywna";
            _mainActionItem.Text =
                MenuText("■ Zatrzymaj prezentację", HotkeyAction.StartStop);
            _controlToggleItem.Text =
                MenuText("↔ Steruj ekranem prezentacji", HotkeyAction.ToggleScreens);
        }
        else
        {
            _statusItem.Text = "● Gotowy";
            _mainActionItem.Text =
                MenuText("▶ Start prezentacji", HotkeyAction.StartStop);
            _controlToggleItem.Text =
                MenuText("↔ Steruj ekranem prezentacji", HotkeyAction.ToggleScreens);
        }

        _dashboard?.RefreshState();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_uiDispatcher.IsDisposed || !_uiDispatcher.IsHandleCreated)
            return;

        try
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                // Display removal/reconfiguration can invalidate both the
                // remembered coordinates and a global ClipCursor rectangle.
                _controller.RefreshMonitors();
                RefreshMenuText();
                RefreshTrayStatus();
                _dashboard?.RefreshState();

                _trayIcon.BalloonTipTitle = AppBranding.DisplayName;
                _trayIcon.BalloonTipText =
                    "Wykryto zmianę układu monitorów. Konfiguracja została odświeżona.";
                _trayIcon.ShowBalloonTip(2200);
            }));
        }
        catch (InvalidOperationException)
        {
            // App is already shutting down.
        }
    }

    private void ExitApplication()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        UnregisterAllHotkeys();

        _controller.Dispose();
        _hotkeyWindow.Dispose();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _uiDispatcher.Dispose();
        _appIcon.Dispose();

        ExitThread();
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "app.ico");

            if (File.Exists(path))
                return new Icon(path);
        }
        catch
        {
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
