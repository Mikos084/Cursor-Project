namespace MultiplePointers;

internal sealed class BrandingForm : Form
{
    public BrandingForm(Icon icon, string currentName,
        Func<string, (bool Success, string? Error)> save)
    {
        Text = "Easter egg — " + AppBranding.DisplayName;
        Icon = icon;
        ClientSize = new Size(620, 470);
        MinimumSize = new Size(580, 470);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10f);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;

        AutoScroll = true;
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(28),
            ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var hint = new Label { Text = "Podaj hasło, aby odblokować własną nazwę aplikacji.", AutoSize = true };
        var password = new TextBox { Width = 480, UseSystemPasswordChar = true, AccessibleName = "Hasło" };
        var unlock = new Button { Text = "Odblokuj", AutoSize = true };
        var name = new TextBox { Text = currentName, Width = 480, MaxLength = AppBranding.MaxNameLength,
            Enabled = false, AccessibleName = "Wyświetlana nazwa aplikacji" };
        var info = new Label { Text = "Własna nazwa zastąpi dotychczasową nazwę w oknie i trayu.", AutoSize = true };
        var message = new Label { AutoSize = true, MinimumSize = new Size(0, 48), ForeColor = AppTheme.Muted };
        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty };
        var apply = new Button { Text = "Zapisz", Enabled = false, AutoSize = true };
        var reset = new Button { Text = "Nazwa domyślna", Enabled = false, AutoSize = true };
        var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.AddRange([apply, reset, cancel]);
        foreach (var control in new Control[] { hint, password, unlock,
                     new Label { Text = "Wyświetlana nazwa", AutoSize = true }, name, info, message, buttons })
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(0, 0, 0, 12);
            layout.Controls.Add(control, 0, row);
        }
        unlock.Dock = DockStyle.None;
        unlock.Anchor = AnchorStyles.Left;
        Controls.Add(layout);
        foreach (var button in new[] { unlock, apply, reset, cancel })
        {
            button.BackColor = AppTheme.SurfaceAlt;
            button.ForeColor = AppTheme.Text;
            button.UseVisualStyleBackColor = false;
            button.MinimumSize = new Size(button == reset ? 170 : 125, 44);
            button.Padding = new Padding(12, 6, 12, 6);
            button.Margin = new Padding(0, 0, 12, 10);
        }
        foreach (var input in new[] { password, name })
        {
            input.BackColor = AppTheme.Surface;
            input.ForeColor = AppTheme.Text;
        }
        AcceptButton = unlock;
        CancelButton = cancel;
        unlock.Click += (_, _) =>
        {
            bool correct = password.Text == "dupa";
            password.Clear();
            if (!correct)
            {
                message.Text = "Niepoprawne hasło.";
                password.Focus();
                return;
            }
            password.Enabled = unlock.Enabled = false;
            name.Enabled = apply.Enabled = reset.Enabled = true;
            message.Text = "Wpisz własną nazwę (maksymalnie 40 znaków).";
            AcceptButton = apply;
            name.Focus();
            name.SelectAll();
        };
        reset.Click += (_, _) => name.Text = AppBranding.DefaultName;
        apply.Click += (_, _) =>
        {
            if (!name.Enabled) return;
            var result = save(name.Text);
            if (!result.Success)
            {
                message.Text = result.Error;
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
    }
}
