namespace MultiplePointers;

internal sealed class DashboardForm : Form
{
    private readonly PointerController _controller;
    private readonly Func<string> _getHotkeyStatus;
    private readonly Func<HotkeyAction, string> _getHotkeyText;
    private readonly Action _showInstruction;
    private readonly Action _showHotkeySettings;

    private readonly ComboBox _presentationScreen = new();
    private readonly ComboBox _controlScreen = new();

    private readonly Label _statusTitle = new();
    private readonly Label _statusDescription = new();
    private readonly Label _routeLabel = new();

    private readonly Label _currentScreen = new();
    private readonly Label _parked = new();
    private readonly Label _locked = new();
    private readonly Label _savedPresentationPoint = new();
    private readonly Label _savedControlPoint = new();
    private readonly Label _hotkeys = new();

    private readonly RoundedButton _mainButton = new();
    private readonly RoundedButton _controlToggleButton = new();
    private readonly RoundedButton _resetButton = new();

    private readonly Label _brandTitle = new();
    private bool _refreshing;

    public DashboardForm(
        PointerController controller,
        Icon icon,
        Func<string> getHotkeyStatus,
        Func<HotkeyAction, string> getHotkeyText,
        Action showInstruction,
        Action showHotkeySettings,
        Action showBranding)
    {
        _controller = controller;
        _getHotkeyStatus = getHotkeyStatus;
        _getHotkeyText = getHotkeyText;
        _showInstruction = showInstruction;
        _showHotkeySettings = showHotkeySettings;

        Text = "Multiple Pointers";
        Icon = icon;

        Width = 1120;
        Height = 860;
        MinimumSize = new Size(760, 600);
        StartPosition = FormStartPosition.CenterScreen;

        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = new Font("Segoe UI", 10f);

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        DoubleBuffered = true;

        BuildUi(showBranding);
        ApplyBranding();
        RefreshState();
    }

    public void ApplyBranding()
    {
        Text = AppBranding.DisplayName;
        _brandTitle.Text = AppBranding.DisplayName;
    }

    private void BuildUi(Action showBranding)
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Padding = new Padding(28, 24, 28, 28),
            BackColor = AppTheme.Background
        };

        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddSection(content, BuildHeader(), 76);
        var egg = new CheckBox { Text = "Easter egg", AutoSize = true, Margin = Padding.Empty };
        egg.CheckedChanged += (_, _) =>
        {
            if (!egg.Checked) return;
            try { showBranding(); }
            finally { egg.Checked = false; }
        };
        AddSection(content, egg, 34);
        AddSection(content, BuildStatusCard(), 94);
        AddSection(content, BuildScreenFlow(), 196);
        AddSection(content, BuildPrimaryAction(), 94);
        AddSection(content, BuildControlToggle(), 100);
        AddSection(content, BuildLiveState(), 132);
        AddSection(content, BuildBottomInfo(), 218);

        Controls.Add(content);
    }

    private static void AddSection(
        TableLayoutPanel root,
        Control control,
        int preferredHeight)
    {
        int row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, preferredHeight));

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 12);

        root.Controls.Add(control, 0, row);
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Background,
            Margin = Padding.Empty
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Background
        };

        titleArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        titleArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _brandTitle.Dock = DockStyle.Fill;
        _brandTitle.ForeColor = AppTheme.Text;
        _brandTitle.Font = new Font("Segoe UI Semibold", 24f);
        _brandTitle.TextAlign = ContentAlignment.MiddleLeft;
        _brandTitle.AutoEllipsis = true;
        _brandTitle.UseMnemonic = false;
        titleArea.Controls.Add(_brandTitle, 0, 0);
        titleArea.Controls.Add(new Label
        {
            Text = "v0.8.3 • stabilność monitorów • własne globalne skróty",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 9.6f),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        }, 0, 1);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppTheme.Background,
            Padding = new Padding(8, 8, 0, 0)
        };

        var hotkeys = SecondaryButton("Ustaw skróty", 128);
        hotkeys.Click += (_, _) => _showHotkeySettings();

        var refresh = SecondaryButton("Odśwież", 104);
        refresh.Click += (_, _) =>
        {
            _controller.RefreshMonitors();
            RefreshState();
        };

        var help = SecondaryButton("Instrukcja", 104);
        help.Click += (_, _) => _showInstruction();

        actions.Controls.Add(hotkeys);
        actions.Controls.Add(refresh);
        actions.Controls.Add(help);

        panel.Controls.Add(titleArea, 0, 0);
        panel.Controls.Add(actions, 1, 0);

        return panel;
    }

    private Control BuildStatusCard()
    {
        var card = new RoundedPanel
        {
            CornerRadius = 18,
            Padding = new Padding(20, 14, 20, 14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));

        var stateText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        stateText.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stateText.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _statusTitle.Dock = DockStyle.Fill;
        _statusTitle.Font = new Font("Segoe UI Semibold", 12.5f);
        _statusTitle.TextAlign = ContentAlignment.MiddleLeft;

        _statusDescription.Dock = DockStyle.Fill;
        _statusDescription.ForeColor = AppTheme.Muted;
        _statusDescription.Font = new Font("Segoe UI", 9.2f);
        _statusDescription.TextAlign = ContentAlignment.TopLeft;
        _statusDescription.AutoEllipsis = true;

        stateText.Controls.Add(_statusTitle, 0, 0);
        stateText.Controls.Add(_statusDescription, 0, 1);

        _resetButton.Dock = DockStyle.Fill;
        _resetButton.Margin = new Padding(16, 8, 0, 8);
        _resetButton.BaseColor = AppTheme.SurfaceAlt;
        _resetButton.HoverColor = AppTheme.SurfaceHover;
        _resetButton.Click += (_, _) =>
        {
            _controller.Reset();
            RefreshState();
        };

        layout.Controls.Add(stateText, 0, 0);
        layout.Controls.Add(_resetButton, 1, 0);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildScreenFlow()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = AppTheme.Background,
            Margin = Padding.Empty
        };

        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 4));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        outer.Controls.Add(
            MakeScreenCard(
                "1",
                "EKRAN UDOSTĘPNIANY",
                "Tu zostaje nieruchoma strzałka.",
                _presentationScreen),
            0,
            0);

        var swapHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Background
        };

        var swap = new RoundedButton
        {
            Text = "⇄",
            Width = 44,
            Height = 44,
            CornerRadius = 12,
            BaseColor = AppTheme.SurfaceAlt,
            HoverColor = AppTheme.SurfaceHover,
            BorderColor = AppTheme.Border,
            Anchor = AnchorStyles.None
        };

        swap.Click += (_, _) => SwapScreens();

        swapHost.Resize += (_, _) =>
        {
            swap.Left = Math.Max(0, (swapHost.ClientSize.Width - swap.Width) / 2);
            swap.Top = Math.Max(0, (swapHost.ClientSize.Height - swap.Height) / 2);
        };

        swapHost.Controls.Add(swap);

        outer.Controls.Add(swapHost, 1, 0);

        outer.Controls.Add(
            MakeScreenCard(
                "2",
                "EKRAN PRYWATNY",
                "Tutaj działa prawdziwa mysz podczas prezentacji.",
                _controlScreen),
            2,
            0);

        _presentationScreen.SelectedIndexChanged += (_, _) =>
        {
            if (!_refreshing &&
                _presentationScreen.SelectedItem is MonitorItem item)
            {
                _controller.SetPresentationScreen(item.DeviceName);
                RefreshState();
            }
        };

        _controlScreen.SelectedIndexChanged += (_, _) =>
        {
            if (!_refreshing &&
                _controlScreen.SelectedItem is MonitorItem item)
            {
                _controller.SetControlScreen(item.DeviceName);
                RefreshState();
            }
        };

        return outer;
    }

    private Control BuildPrimaryAction()
    {
        var card = new RoundedPanel
        {
            CornerRadius = 18,
            Padding = new Padding(18, 12, 18, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        left.Controls.Add(new Label
        {
            Text = "3  Ustaw prawdziwy kursor na ekranie udostępnianym",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.3f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _routeLabel.Dock = DockStyle.Fill;
        _routeLabel.ForeColor = AppTheme.Muted;
        _routeLabel.Font = new Font("Segoe UI", 9f);
        _routeLabel.TextAlign = ContentAlignment.TopLeft;
        _routeLabel.AutoEllipsis = true;
        left.Controls.Add(_routeLabel, 0, 1);

        _mainButton.Dock = DockStyle.Fill;
        _mainButton.Margin = new Padding(14, 4, 0, 4);
        _mainButton.CornerRadius = 14;
        _mainButton.BorderThickness = 0;
        _mainButton.Font = new Font("Segoe UI Semibold", 11f);
        _mainButton.Click += (_, _) => TogglePresentation();

        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(_mainButton, 1, 0);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildControlToggle()
    {
        var card = new RoundedPanel
        {
            CornerRadius = 18,
            Padding = new Padding(18, 12, 18, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));

        var text = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        text.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        text.Controls.Add(new Label
        {
            Text = "Sterowanie prezentacją bez kończenia sesji",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.5f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        text.Controls.Add(new Label
        {
            Text = "Każdy monitor pamięta własne X/Y. Przełączenie zapisuje ekran opuszczany i przywraca pozycję ekranu docelowego.",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 8.9f),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        }, 0, 1);

        _controlToggleButton.Dock = DockStyle.Fill;
        _controlToggleButton.Margin = new Padding(14, 4, 0, 4);
        _controlToggleButton.CornerRadius = 13;
        _controlToggleButton.Click += (_, _) =>
            Run(_controller.TogglePresentationControl);

        layout.Controls.Add(text, 0, 0);
        layout.Controls.Add(_controlToggleButton, 1, 0);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildLiveState()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = AppTheme.Background,
            Margin = Padding.Empty
        };

        for (int i = 0; i < 5; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

        grid.Controls.Add(MakeMetricCard("PRAWDZIWA MYSZ", _currentScreen), 0, 0);
        grid.Controls.Add(MakeMetricCard("STRZAŁKA", _parked), 1, 0);
        grid.Controls.Add(MakeMetricCard("BLOKADA", _locked), 2, 0);
        grid.Controls.Add(MakeMetricCard("POZYCJA PREZENTACJI", _savedPresentationPoint), 3, 0);
        grid.Controls.Add(MakeMetricCard("POZYCJA PRYWATNA", _savedControlPoint), 4, 0);

        return grid;
    }

    private Control BuildBottomInfo()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Background,
            Margin = Padding.Empty
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var hotkeyCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 18,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 0, 6, 0)
        };

        var hk = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        hk.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        hk.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        hk.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        hk.Controls.Add(new Label
        {
            Text = "Skróty globalne",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.5f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var edit = SecondaryButton("Edytuj skróty", 140);
        edit.Dock = DockStyle.Fill;
        edit.Margin = new Padding(8, 0, 0, 0);
        edit.Click += (_, _) => _showHotkeySettings();
        hk.Controls.Add(edit, 1, 0);

        _hotkeys.Dock = DockStyle.Fill;
        _hotkeys.ForeColor = AppTheme.Muted;
        _hotkeys.Font = new Font("Consolas", 8.5f);
        _hotkeys.TextAlign = ContentAlignment.TopLeft;
        _hotkeys.AutoEllipsis = true;
        hk.Controls.Add(_hotkeys, 0, 1);
        hk.SetColumnSpan(_hotkeys, 2);

        hotkeyCard.Controls.Add(hk);

        var captureCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 18,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(6, 0, 0, 0)
        };

        var capture = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        capture.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        capture.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        capture.Controls.Add(new Label
        {
            Text = "Google Meet / OBS",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.5f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        capture.Controls.Add(new Label
        {
            Text =
                "Meet: udostępniaj cały ekran wybrany jako prezentacyjny.\n" +
                "OBS: Display Capture → ten sam monitor → Show Cursor = OFF.\n" +
                "W razie problemu użyj skrótu Reset.",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);

        captureCard.Controls.Add(capture);

        grid.Controls.Add(hotkeyCard, 0, 0);
        grid.Controls.Add(captureCard, 1, 0);

        return grid;
    }

    public void RefreshState()
    {
        if (IsDisposed)
            return;

        _refreshing = true;

        try
        {
            RefreshMonitorCombos();

            // Changing the logical monitor roles while a session is active
            // can invalidate the saved cursor coordinates and ClipCursor area.
            // Keep monitor selection immutable until Reset/Stop.
            bool canEditMonitors = !_controller.IsSessionActive;
            _presentationScreen.Enabled = canEditMonitors;
            _controlScreen.Enabled = canEditMonitors;

            _currentScreen.Text = FriendlyName(_controller.CurrentScreenName);

            _parked.Text = _controller.IsControllingPresentation
                ? "prawdziwy kursor aktywny"
                : _controller.ParkedScreenName is null
                    ? "—"
                    : FriendlyName(_controller.ParkedScreenName);

            _locked.Text = _controller.LockedScreenName is null
                ? "—"
                : FriendlyName(_controller.LockedScreenName);

            Point? savedPresentation = _controller.SavedPresentationPosition;
            _savedPresentationPoint.Text = savedPresentation.HasValue
                ? $"X={savedPresentation.Value.X}, Y={savedPresentation.Value.Y}"
                : "—";

            Point? savedControl = _controller.SavedControlPosition;
            _savedControlPoint.Text = savedControl.HasValue
                ? $"X={savedControl.Value.X}, Y={savedControl.Value.Y}"
                : "—";

            _hotkeys.Text = _getHotkeyStatus();

            _resetButton.Text =
                $"RESET   {_getHotkeyText(HotkeyAction.Reset)}";

            string shared = FriendlyName(_controller.PresentationScreenName);
            string privateScreen = FriendlyName(_controller.ControlScreenName);

            _routeLabel.Text =
                $"Pozycja zostanie zapisana na {shared}, a prawdziwy kursor przejdzie na {privateScreen}.";

            string toggleShortcut = _getHotkeyText(HotkeyAction.ToggleScreens);
            string startShortcut = _getHotkeyText(HotkeyAction.StartStop);

            if (_controller.IsControllingPresentation)
            {
                _statusTitle.Text = "● Sterujesz ekranem prezentacji";
                _statusTitle.ForeColor = AppTheme.Accent;
                _statusDescription.Text =
                    "Prawdziwy kursor jest na prezentacji. Po przełączeniu wróci do ostatniego X/Y ekranu prywatnego.";

                _controlToggleButton.Text =
                    $"Wróć na prywatny   {toggleShortcut}";
                _controlToggleButton.BaseColor = AppTheme.Accent;
                _controlToggleButton.HoverColor = AppTheme.AccentHover;
                _controlToggleButton.BorderColor = AppTheme.Accent;
            }
            else
            {
                _controlToggleButton.Text =
                    $"Steruj prezentacją   {toggleShortcut}";
                _controlToggleButton.BaseColor = AppTheme.SurfaceAlt;
                _controlToggleButton.HoverColor = AppTheme.SurfaceHover;
                _controlToggleButton.BorderColor = AppTheme.Border;

                if (_controller.IsSessionActive)
                {
                    _statusTitle.Text = "● Prezentacja aktywna";
                    _statusTitle.ForeColor = AppTheme.Success;
                    _statusDescription.Text =
                        "Nieruchoma strzałka jest na prezentacji, a prawdziwy kursor pracuje na ekranie prywatnym.";
                }
                else
                {
                    _statusTitle.Text = "● Gotowy do prezentacji";
                    _statusTitle.ForeColor = AppTheme.Success;
                    _statusDescription.Text =
                        "Wybierz dwa monitory, ustaw kursor na ekranie udostępnianym i uruchom prezentację.";
                }
            }

            if (_controller.IsSessionActive)
            {
                _mainButton.Text =
                    $"■  ZATRZYMAJ PREZENTACJĘ   {startShortcut}";
                _mainButton.BaseColor = AppTheme.Danger;
                _mainButton.HoverColor = AppTheme.DangerHover;
                _mainButton.BorderColor = AppTheme.Danger;
            }
            else
            {
                _mainButton.Text =
                    $"▶  START PREZENTACJI   {startShortcut}";
                _mainButton.BaseColor = AppTheme.Accent;
                _mainButton.HoverColor = AppTheme.AccentHover;
                _mainButton.BorderColor = AppTheme.Accent;
            }

            _mainButton.Invalidate();
            _controlToggleButton.Invalidate();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void TogglePresentation()
    {
        if (_controller.IsSessionActive)
        {
            _controller.Reset();
            RefreshState();
            return;
        }

        Run(_controller.ParkAndGoToControlScreen);
    }

    private void SwapScreens()
    {
        if (_controller.IsSessionActive)
        {
            MessageBox.Show(
                "Najpierw zatrzymaj prezentację. Role monitorów można zamieniać tylko wtedy, gdy sesja nie jest aktywna.",
                AppBranding.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (_presentationScreen.SelectedItem is not MonitorItem left ||
            _controlScreen.SelectedItem is not MonitorItem right)
        {
            return;
        }

        string oldPresentation = left.DeviceName;
        string oldControl = right.DeviceName;

        _controller.SetPresentationScreen(oldControl);
        _controller.SetControlScreen(oldPresentation);

        RefreshState();
    }

    private Control MakeScreenCard(
        string number,
        string title,
        string hint,
        ComboBox combo)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 18,
            Padding = new Padding(18, 14, 18, 14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var badge = new Label
        {
            Text = number,
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 10, 6)
        };

        layout.Controls.Add(badge, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 1, 0);

        var hintLabel = new Label
        {
            Text = hint,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 8.8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        layout.Controls.Add(hintLabel, 0, 1);
        layout.SetColumnSpan(hintLabel, 2);

        combo.Dock = DockStyle.Top;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = AppTheme.SurfaceAlt;
        combo.ForeColor = AppTheme.Text;
        combo.Font = new Font("Segoe UI", 9.5f);
        combo.DropDownWidth = 620;
        combo.Margin = new Padding(0, 7, 0, 0);

        layout.Controls.Add(combo, 0, 2);
        layout.SetColumnSpan(combo, 2);

        card.Controls.Add(layout);
        return card;
    }

    private static Control MakeMetricCard(
        string title,
        Label value)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 15,
            Padding = new Padding(13, 10, 13, 10),
            Margin = new Padding(4)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI Semibold", 7.8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 0);

        value.Dock = DockStyle.Fill;
        value.ForeColor = AppTheme.Text;
        value.Font = new Font("Segoe UI Semibold", 9.2f);
        value.TextAlign = ContentAlignment.TopLeft;
        value.AutoEllipsis = true;

        layout.Controls.Add(value, 0, 1);
        card.Controls.Add(layout);

        return card;
    }

    private static RoundedButton SecondaryButton(
        string text,
        int width)
    {
        return new RoundedButton
        {
            Text = text,
            Width = width,
            Height = 38,
            CornerRadius = 11,
            BaseColor = AppTheme.SurfaceAlt,
            HoverColor = AppTheme.SurfaceHover,
            BorderColor = AppTheme.Border,
            Margin = new Padding(6, 0, 0, 0)
        };
    }

    private void RefreshMonitorCombos()
    {
        var monitors = _controller.GetMonitors();

        FillCombo(
            _presentationScreen,
            monitors,
            _controller.PresentationScreenName);

        FillCombo(
            _controlScreen,
            monitors,
            _controller.ControlScreenName);
    }

    private static void FillCombo(
        ComboBox combo,
        IReadOnlyList<MonitorDescriptor> monitors,
        string? selectedName)
    {
        combo.BeginUpdate();
        combo.Items.Clear();

        foreach (var monitor in monitors)
        {
            combo.Items.Add(new MonitorItem(
                monitor.DeviceName,
                monitor.FullLabel));
        }

        int index = -1;

        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is MonitorItem item &&
                string.Equals(
                    item.DeviceName,
                    selectedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
            combo.SelectedIndex = index;
        else if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;

        combo.EndUpdate();
    }

    private string FriendlyName(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return "—";

        var monitor = _controller.GetMonitors().FirstOrDefault(
            m => string.Equals(
                m.DeviceName,
                deviceName,
                StringComparison.OrdinalIgnoreCase));

        return monitor is null
            ? deviceName
            : $"{monitor.ShortName} — {monitor.FriendlyName}";
    }

    private delegate bool PointerAction(out string? error);

    private void Run(PointerAction action)
    {
        if (!action(out string? error) &&
            !string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show(
                error,
                AppBranding.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        RefreshState();
    }

    private sealed class MonitorItem
    {
        public string DeviceName { get; }
        private string Label { get; }

        public MonitorItem(string deviceName, string label)
        {
            DeviceName = deviceName;
            Label = label;
        }

        public override string ToString()
            => Label;
    }
}
