namespace MultiplePointers;

internal sealed class BrandingForm : Form
{
    public BrandingForm(Icon icon, string currentName,
        Func<string, (bool Success, string? Error)> save)
    {
        Text = "Easter egg — " + AppBranding.DisplayName;
        Icon = icon;
        ClientSize = new Size(540, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10f);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20),
            FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var hint = new Label { Text = "Podaj hasło, aby odblokować własną nazwę aplikacji.", AutoSize = true };
        var password = new TextBox { Width = 480, UseSystemPasswordChar = true, AccessibleName = "Hasło" };
        var unlock = new Button { Text = "Odblokuj", AutoSize = true };
        var name = new TextBox { Text = currentName, Width = 480, MaxLength = AppBranding.MaxNameLength,
            Enabled = false, AccessibleName = "Wyświetlana nazwa aplikacji" };
        var info = new Label { Text = "Własna nazwa otrzyma dopisek „— Multiple Pointers”.", AutoSize = true };
        var message = new Label { Width = 480, Height = 45, ForeColor = AppTheme.Muted };
        var buttons = new FlowLayoutPanel { Width = 480, Height = 40 };
        var apply = new Button { Text = "Zapisz", Enabled = false, AutoSize = true };
        var reset = new Button { Text = "Nazwa domyślna", Enabled = false, AutoSize = true };
        var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.AddRange([apply, reset, cancel]);
        layout.Controls.AddRange([hint, password, unlock, name, info, message, buttons]);
        Controls.Add(layout);
        foreach (var button in new[] { unlock, apply, reset, cancel })
        {
            button.BackColor = AppTheme.SurfaceAlt;
            button.ForeColor = AppTheme.Text;
            button.UseVisualStyleBackColor = false;
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
