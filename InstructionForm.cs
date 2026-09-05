namespace MultiplePointers;

internal sealed class InstructionForm : Form
{
    private static readonly Color Bg = Color.FromArgb(18, 20, 25);
    private static readonly Color Card = Color.FromArgb(29, 32, 39);
    private static readonly Color TextMain = Color.FromArgb(246, 247, 250);
    private static readonly Color TextMuted = Color.FromArgb(174, 181, 193);
    private static readonly Color Accent = Color.FromArgb(102, 166, 255);

    public InstructionForm(
        Icon icon,
        Func<HotkeyAction, string> shortcut)
    {
        Text = "Jak używać — Multiple Pointers";
        Icon = icon;
        Width = 810;
        Height = 720;
        MinimumSize = new Size(650, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 10f);
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        DoubleBuffered = true;

        BuildUi(shortcut);
    }

    private void BuildUi(Func<HotkeyAction, string> shortcut)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(28),
            BackColor = Bg
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg
        };

        header.Controls.Add(new Label
        {
            Text = "Instrukcja — dwa ekrany, dwie pozycje",
            ForeColor = TextMain,
            Font = new Font("Segoe UI Semibold", 22f),
            AutoSize = true,
            Location = new Point(0, 0)
        });

        header.Controls.Add(new Label
        {
            Text = "v0.8.3 • kliknij „Ustaw skróty”, a potem pole kombinacji i naciśnij własne klawisze.",
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(3, 48)
        });

        root.Controls.Add(header, 0, 0);

        var box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Card,
            ForeColor = TextMain,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            DetectUrls = false,
            Font = new Font("Segoe UI", 10.5f),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = new Padding(0, 0, 0, 12)
        };

        box.Text =
$@"NAJWAŻNIEJSZA ZASADA

Każdy ekran ma własną pamięć kursora.

EKRAN PREZENTACJI pamięta swoje X/Y.
EKRAN PRYWATNY pamięta swoje X/Y.


START

1. Wybierz ekran udostępniany.
2. Wybierz ekran prywatny.
3. Ustaw kursor na prezentacji tam, gdzie ma zostać strzałka.
4. Użyj: {shortcut(HotkeyAction.StartStop)}

Program zapisuje pozycję prezentacji, zostawia strzałkę
oraz przechodzi na ekran prywatny.


PRZEŁĄCZANIE MIĘDZY EKRANAMI

Użyj: {shortcut(HotkeyAction.ToggleScreens)}

Program zawsze:
1. zapisuje dokładne X/Y ekranu, który opuszczasz,
2. przywraca dokładne X/Y ekranu, na który wracasz.

Dzięki temu oba monitory mają niezależną pamięć pozycji.


POZOSTAŁE SKRÓTY

Parkuj / usuń strzałkę:
{shortcut(HotkeyAction.TogglePark)}

Reset / odblokuj:
{shortcut(HotkeyAction.Reset)}

Otwórz panel:
{shortcut(HotkeyAction.OpenPanel)}

Odśwież monitory:
{shortcut(HotkeyAction.RefreshMonitors)}

Zamień role monitorów:
{shortcut(HotkeyAction.SwapMonitorRoles)}

Otwórz instrukcję:
{shortcut(HotkeyAction.OpenInstructions)}

Otwórz ustawienia skrótów:
{shortcut(HotkeyAction.OpenHotkeySettings)}


ZMIANA SKRÓTÓW

Kliknij „Ustaw skróty” w głównym panelu albo:
prawy klik na ikonie Multiple Pointers przy zegarze
→ Ustawienia skrótów...

W edytorze kliknij duże pole „Kliknij i naciśnij skrót”
przy wybranej funkcji, a następnie naciśnij własną kombinację.

Możesz:
• zmienić kombinację każdej funkcji,
• wyłączyć wybrany skrót,
• przywrócić ustawienia domyślne.

Program sprawdza nowe kombinacje przed zapisaniem.
Jeżeli skrót jest zajęty, wraca do poprzedniego poprawnego zestawu.


GOOGLE MEET

Zaprezentuj teraz
→ Cały ekran
→ wybierz ekran ustawiony jako EKRAN UDOSTĘPNIANY.


OBS

Display Capture
→ ekran udostępniany
→ Show Cursor = OFF.";

        root.Controls.Add(box, 0, 1);

        var close = new Button
        {
            Text = "Rozumiem",
            Width = 130,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 8, 0, 0)
        };

        close.FlatAppearance.BorderSize = 0;
        close.Click += (_, _) => Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Bg
        };

        footer.Controls.Add(close);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }
}
