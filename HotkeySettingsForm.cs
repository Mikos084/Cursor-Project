namespace MultiplePointers;

internal sealed class HotkeySettingsForm : Form
{
    private readonly Func<AppSettings, (bool Success, string? Error)> _apply;
    private AppSettings _working;

    private readonly Dictionary<HotkeyAction, HotkeyRow> _rows = new();
    private readonly Label _message = new();

    public HotkeySettingsForm(
        Icon icon,
        AppSettings current,
        Func<AppSettings, (bool Success, string? Error)> apply)
    {
        _working = current.Clone();
        _apply = apply;

        Text = "Skróty globalne — Multiple Pointers";
        Icon = icon;
        Width = 900;
        Height = 690;
        MinimumSize = new Size(680, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = new Font("Segoe UI", 10f);
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        BuildUi();
        LoadRows();
    }

    private void BuildUi()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 108,
            BackColor = AppTheme.Background,
            Padding = new Padding(28, 22, 28, 8)
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Background
        };
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        headerLayout.Controls.Add(new Label
        {
            Text = "Skróty globalne",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 23f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        headerLayout.Controls.Add(new Label
        {
            Text = "Kliknij pole po prawej i po prostu naciśnij własną kombinację. " +
                   "Backspace/Delete czyści pole. Esc kończy przechwytywanie.",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 9.8f),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);

        header.Controls.Add(headerLayout);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 104,
            BackColor = AppTheme.Background,
            Padding = new Padding(28, 8, 28, 20)
        };

        var footerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = AppTheme.Background
        };
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _message.Dock = DockStyle.Fill;
        _message.ForeColor = AppTheme.Muted;
        _message.TextAlign = ContentAlignment.MiddleLeft;
        _message.AutoEllipsis = true;
        footerLayout.Controls.Add(_message, 0, 0);
        footerLayout.SetColumnSpan(_message, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = AppTheme.Background
        };

        var save = MakeButton("Zapisz zmiany", true);
        save.Width = 150;
        save.Click += (_, _) => SaveSettings();

        var cancel = MakeButton("Anuluj", false);
        cancel.Width = 110;
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var defaults = MakeButton("Przywróć domyślne", false);
        defaults.Width = 170;
        defaults.Click += (_, _) =>
        {
            _working = AppSettings.Defaults();
            LoadRows();
            SetMessage(
                "Domyślne kombinacje zostały wczytane do edytora. Kliknij „Zapisz zmiany”, aby je zastosować.",
                false);
        };

        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        actions.Controls.Add(defaults);
        footerLayout.Controls.Add(actions, 1, 1);

        footer.Controls.Add(footerLayout);

        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.Background,
            Padding = new Padding(28, 6, 28, 12)
        };

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = AppTheme.Background,
            Padding = new Padding(0, 0, 0, 8)
        };

        foreach (HotkeyAction action in Enum.GetValues<HotkeyAction>())
        {
            HotkeyRow row = CreateRow(action);
            _rows[action] = row;

            int rowIndex = list.RowCount++;
            list.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            list.Controls.Add(row.Container, 0, rowIndex);
        }

        viewport.Controls.Add(list);

        Controls.Add(viewport);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private HotkeyRow CreateRow(HotkeyAction action)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            CornerRadius = 16,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(18, 12, 18, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));

        var textArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        textArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        textArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        textArea.Controls.Add(new Label
        {
            Text = AppSettings.ActionName(action),
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Semibold", 10.5f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        textArea.Controls.Add(new Label
        {
            Text = ActionDescription(action),
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 8.8f),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        }, 0, 1);

        var capture = new HotkeyCaptureBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 8, 8, 8)
        };

        var enabled = new CheckBox
        {
            Text = "Włączony",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            CheckAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(8)
        };

        var clear = MakeButton("Wyczyść", false);
        clear.Dock = DockStyle.Fill;
        clear.Margin = new Padding(6, 8, 0, 8);

        layout.Controls.Add(textArea, 0, 0);
        layout.Controls.Add(capture, 1, 0);
        layout.Controls.Add(enabled, 2, 0);
        layout.Controls.Add(clear, 3, 0);

        card.Controls.Add(layout);

        var row = new HotkeyRow(action, card, enabled, capture, clear);

        enabled.CheckedChanged += (_, _) =>
        {
            HotkeyBinding binding = _working.Get(action);
            binding.Enabled = enabled.Checked;
            capture.Enabled = enabled.Checked;
            clear.Enabled = enabled.Checked;
            RefreshRow(row);
        };

        capture.BindingChanged += (_, _) =>
        {
            HotkeyBinding captured = capture.GetBinding();
            HotkeyBinding target = _working.Get(action);

            target.Enabled = true;
            target.Ctrl = captured.Ctrl;
            target.Alt = captured.Alt;
            target.Shift = captured.Shift;
            target.Key = captured.Key;

            if (!enabled.Checked)
                enabled.Checked = true;

            SetMessage(
                $"Nowy skrót dla „{AppSettings.ActionName(action)}”: {target.DisplayText}. " +
                "Kliknij „Zapisz zmiany”, aby go aktywować.",
                false);
        };

        clear.Click += (_, _) =>
        {
            HotkeyBinding target = _working.Get(action);
            target.Key = Keys.None;
            target.Ctrl = false;
            target.Alt = false;
            target.Shift = false;
            capture.SetBinding(target);
            SetMessage(
                $"Wyczyszczono skrót dla „{AppSettings.ActionName(action)}”.",
                false);
        };

        return row;
    }

    private void LoadRows()
    {
        foreach (var pair in _rows)
        {
            HotkeyBinding binding = _working.Get(pair.Key);
            pair.Value.Enabled.Checked = binding.Enabled;
            pair.Value.Capture.SetBinding(binding);
            RefreshRow(pair.Value);
        }
    }

    private void RefreshRow(HotkeyRow row)
    {
        HotkeyBinding binding = _working.Get(row.Action);
        row.Capture.SetBinding(binding);
        row.Capture.Enabled = binding.Enabled;
        row.Clear.Enabled = binding.Enabled;
    }

    private void SaveSettings()
    {
        string? validationError = Validate(_working);
        if (validationError is not null)
        {
            SetMessage(validationError, true);
            return;
        }

        var result = _apply(_working.Clone());

        if (!result.Success)
        {
            SetMessage(
                result.Error ?? "Nie udało się zastosować skrótów.",
                true);
            return;
        }

        SetMessage("Skróty zapisane i zarejestrowane globalnie.", false);
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string? Validate(AppSettings settings)
    {
        var used = new Dictionary<string, HotkeyAction>();

        foreach (var (action, binding) in settings.Enumerate())
        {
            if (!binding.Enabled)
                continue;

            if (binding.Key == Keys.None)
            {
                return
                    $"Ustaw skrót dla „{AppSettings.ActionName(action)}” " +
                    "albo wyłącz tę funkcję.";
            }

            string canonical = binding.CanonicalKey();

            if (used.TryGetValue(canonical, out HotkeyAction other))
            {
                return
                    $"Skrót {binding.DisplayText} jest przypisany jednocześnie do: " +
                    $"„{AppSettings.ActionName(other)}” i „{AppSettings.ActionName(action)}”.";
            }

            used[canonical] = action;
        }

        return null;
    }

    private void SetMessage(string text, bool error)
    {
        _message.Text = text;
        _message.ForeColor = error ? AppTheme.Danger : AppTheme.Muted;
    }

    private static RoundedButton MakeButton(string text, bool primary)
    {
        return new RoundedButton
        {
            Text = text,
            BaseColor = primary ? AppTheme.Accent : AppTheme.SurfaceAlt,
            HoverColor = primary ? AppTheme.AccentHover : AppTheme.SurfaceHover,
            BorderColor = primary ? AppTheme.Accent : AppTheme.Border,
            ForeColor = Color.White,
            CornerRadius = 11
        };
    }

    private static string ActionDescription(HotkeyAction action)
        => action switch
        {
            HotkeyAction.ToggleScreens =>
                "Przełącza prawdziwy kursor między prezentacją a ekranem prywatnym.",
            HotkeyAction.TogglePark =>
                "Zostawia lub usuwa nieruchomą strzałkę na ekranie prezentacji.",
            HotkeyAction.StartStop =>
                "Uruchamia albo zatrzymuje pełny tryb prezentacji.",
            HotkeyAction.Reset =>
                "Awaryjnie usuwa strzałkę i zwalnia blokadę kursora.",
            HotkeyAction.OpenPanel =>
                "Otwiera główne okno Multiple Pointers.",
            HotkeyAction.RefreshMonitors =>
                "Ponownie wykrywa podłączone monitory.",
            HotkeyAction.SwapMonitorRoles =>
                "Zamienia wybrany ekran prezentacyjny z ekranem prywatnym.",
            HotkeyAction.OpenInstructions =>
                "Otwiera instrukcję krok po kroku.",
            HotkeyAction.OpenHotkeySettings =>
                "Otwiera ten edytor skrótów globalnych.",
            _ => ""
        };

    private sealed record HotkeyRow(
        HotkeyAction Action,
        RoundedPanel Container,
        CheckBox Enabled,
        HotkeyCaptureBox Capture,
        RoundedButton Clear);
}
