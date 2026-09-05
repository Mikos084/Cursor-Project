using MultiplePointers;
using System.Reflection;

internal static class Checks
{
    private static int _count;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        _count++;
    }

    private static IEnumerable<Control> Descendants(Control parent)
        => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));

    private static void Click(Control control)
        => typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, [EventArgs.Empty]);

    [STAThread]
    private static void Main()
    {
        foreach (string? bad in new string?[] { null, "", "  ", new('a', 41), "a\nb", "a\u202Eb",
                     "a&b", "foo.exe", "Windows Security", "svchost", "Menedżer zadań", "SYSTEM" })
            Check(!AppBranding.TryValidate(bad, out _, out _), "Rejected unsafe name");
        Check(AppBranding.TryValidate("  Mój pokaz  ", out var valid, out _) && valid == "Mój pokaz", "Trim Unicode name");
        Check(AppBranding.Format(new string('a', 40)).Length <= 63, "Tray length limit");
        Check(AppBranding.Format(valid).EndsWith(" — Multiple Pointers"), "Explicit app identity");
        Check(AppBranding.Format(AppBranding.DefaultName) == AppBranding.DefaultName, "Default identity");

        string path = SettingsStore.SettingsPath;
        Check(!File.Exists(path) && !File.Exists(path + ".bak"), "Use a clean test output directory");
        try
        {
            File.WriteAllText(path, "{}");
            Check(SettingsStore.Load().DisplayName == AppBranding.DefaultName, "Legacy settings");
            File.WriteAllText(path, "{\"DisplayName\":\"svchost\"}");
            Check(SettingsStore.Load().DisplayName == AppBranding.DefaultName, "Validate disk input");
            var settings = AppSettings.Defaults();
            settings.DisplayName = valid;
            settings.TogglePark.Key = Keys.F2;
            Check(settings.Clone().DisplayName == valid, "Clone keeps name");
            Check(SettingsStore.Save(settings, out _), "Save name");
            Check(SettingsStore.Load().DisplayName == valid && SettingsStore.Load().TogglePark.Key == Keys.F2, "Round trip keeps shortcuts");
            Check(SettingsStore.Save(settings, out _), "Create known-good backup");
            File.WriteAllText(path, "invalid json");
            Check(SettingsStore.Load().DisplayName == valid, "Recover backup");
            Directory.CreateDirectory(path + ".tmp");
            Check(!SettingsStore.Save(settings, out var error) && error is not null, "Report save failure");
            Check(SettingsStore.Load().DisplayName == valid, "Failed save preserves backup");
            Directory.Delete(path + ".tmp");
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".bak");
        }

        int saves = 0;
        using var form = new BrandingForm(SystemIcons.Application, valid, _ => { saves++; return (false, "Test failure"); });
        var controls = Descendants(form).ToArray();
        var password = controls.OfType<TextBox>().Single(c => c.AccessibleName == "Hasło");
        var name = controls.OfType<TextBox>().Single(c => c != password);
        var unlock = controls.Single(c => c.Text == "Odblokuj");
        var apply = controls.Single(c => c.Text == "Zapisz");
        Check(!name.Enabled && !apply.Enabled && password.UseSystemPasswordChar, "Locked initially");
        Click(apply);
        Check(saves == 0, "Cannot save before unlock");
        password.Text = "wrong";
        Click(unlock);
        Check(!name.Enabled && password.Text == "", "Wrong password stays locked and clears input");
        password.Text = "dupa";
        Click(unlock);
        Check(name.Enabled && apply.Enabled && password.Text == "", "Correct password unlocks");
        Click(apply);
        Check(saves == 1 && form.DialogResult != DialogResult.OK, "Failed save keeps editor open");
        Click(controls.Single(c => c.Text == "Nazwa domyślna"));
        Check(name.Text == AppBranding.DefaultName, "Reset name");
        using var reopened = new BrandingForm(SystemIcons.Application, valid, _ => (true, null));
        Check(!Descendants(reopened).OfType<TextBox>().Single(c => c.AccessibleName != "Hasło").Enabled, "Reopen requires password");

        AppBranding.Name = valid;
        using var instructions = new InstructionForm(SystemIcons.Application, _ => "F1");
        Check(instructions.Text.Contains(AppBranding.DisplayName), "Instruction title");
        AppBranding.Name = "Druga nazwa";
        instructions.ApplyBranding();
        Check(Descendants(instructions).OfType<RichTextBox>().Single().Text.Contains(AppBranding.DisplayName), "Live instruction branding");
        AppSettings? saved = null;
        using var hotkeys = new HotkeySettingsForm(SystemIcons.Application, new AppSettings { DisplayName = valid }, s => { saved = s; return (true, null); });
        var hotkeyControls = Descendants(hotkeys).ToArray();
        Click(hotkeyControls.Single(c => c.Text == "Przywróć domyślne"));
        Click(hotkeyControls.Single(c => c.Text == "Zapisz zmiany"));
        Check(saved?.DisplayName == valid, "Reset shortcuts preserves branding");
        Console.WriteLine($"Passed {_count} branding checks.");
    }
}
