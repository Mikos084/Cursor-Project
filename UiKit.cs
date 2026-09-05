using System.Drawing.Drawing2D;

namespace MultiplePointers;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(15, 17, 22);
    public static readonly Color Surface = Color.FromArgb(25, 28, 35);
    public static readonly Color SurfaceAlt = Color.FromArgb(33, 37, 46);
    public static readonly Color SurfaceHover = Color.FromArgb(42, 47, 58);
    public static readonly Color Border = Color.FromArgb(52, 58, 70);
    public static readonly Color Text = Color.FromArgb(246, 248, 251);
    public static readonly Color Muted = Color.FromArgb(163, 172, 186);
    public static readonly Color Accent = Color.FromArgb(100, 166, 255);
    public static readonly Color AccentHover = Color.FromArgb(119, 179, 255);
    public static readonly Color Success = Color.FromArgb(83, 205, 142);
    public static readonly Color Danger = Color.FromArgb(222, 86, 94);
    public static readonly Color DangerHover = Color.FromArgb(235, 103, 110);
}

internal static class RoundedGeometry
{
    public static GraphicsPath Path(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();

        if (rect.Width <= 0 || rect.Height <= 0)
            return path;

        int r = Math.Max(1, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        int d = r * 2;

        var arc = new Rectangle(rect.X, rect.Y, d, d);

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}

internal sealed class RoundedPanel : Panel
{
    public int CornerRadius { get; set; } = 16;
    public Color BorderColor { get; set; } = AppTheme.Border;
    public int BorderThickness { get; set; } = 1;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.Surface;
        Padding = new Padding(18);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);

        using var path = RoundedGeometry.Path(
            new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
            CornerRadius);

        Region?.Dispose();
        Region = new Region(path);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = RoundedGeometry.Path(
            new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)),
            CornerRadius);

        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillPath(fill, path);

        if (BorderThickness > 0)
        {
            using var pen = new Pen(BorderColor, BorderThickness);
            e.Graphics.DrawPath(pen, path);
        }

        base.OnPaint(e);
    }
}

internal sealed class RoundedButton : Button
{
    private bool _hovered;

    public int CornerRadius { get; set; } = 12;
    public Color BaseColor { get; set; } = AppTheme.SurfaceAlt;
    public Color HoverColor { get; set; } = AppTheme.SurfaceHover;
    public Color BorderColor { get; set; } = AppTheme.Border;
    public int BorderThickness { get; set; } = 1;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = AppTheme.Text;
        Cursor = Cursors.Hand;
        Height = 42;
        Font = new Font("Segoe UI Semibold", 9.5f);
        UseVisualStyleBackColor = false;
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedGeometry.Path(rect, CornerRadius);

        Color fillColor = Enabled
            ? (_hovered ? HoverColor : BaseColor)
            : Color.FromArgb(28, 31, 38);

        using var fill = new SolidBrush(fillColor);
        pevent.Graphics.FillPath(fill, path);

        if (BorderThickness > 0)
        {
            using var pen = new Pen(BorderColor, BorderThickness);
            pevent.Graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            rect,
            Enabled ? ForeColor : AppTheme.Muted,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            var focusRect = Rectangle.Inflate(rect, -4, -4);
            ControlPaint.DrawFocusRectangle(pevent.Graphics, focusRect);
        }
    }
}

internal sealed class HotkeyCaptureBox : Control
{
    private HotkeyBinding _binding = new();
    private bool _capturing;
    private bool _hovered;

    public event EventHandler? BindingChanged;

    public HotkeyCaptureBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.Selectable,
            true);

        TabStop = true;
        Height = 46;
        MinimumSize = new Size(220, 46);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 10f);
        BackColor = AppTheme.SurfaceAlt;
        ForeColor = AppTheme.Text;
    }

    public void SetBinding(HotkeyBinding binding)
    {
        _binding = binding.Clone();
        Invalidate();
    }

    public HotkeyBinding GetBinding()
        => _binding.Clone();

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        _capturing = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        _capturing = true;
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _capturing = false;
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!Focused || !Enabled || !_capturing)
            return base.ProcessCmdKey(ref msg, keyData);

        if (TryCapture(keyData))
            return true;

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Focused && Enabled && _capturing && TryCapture(e.KeyData))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private bool TryCapture(Keys keyData)
    {
        Keys keyCode = keyData & Keys.KeyCode;

        // Plain Tab remains normal focus navigation.
        if (keyCode == Keys.Tab &&
            (keyData & Keys.Modifiers) == Keys.None)
        {
            return false;
        }

        // Esc exits capture without changing the current binding.
        if (keyCode == Keys.Escape)
        {
            _capturing = false;
            Parent?.Focus();
            Invalidate();
            return true;
        }

        // Modifier keys by themselves are not complete shortcuts.
        if (keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
            return true;

        // Backspace/Delete clear the field.
        if (keyCode is Keys.Back or Keys.Delete)
        {
            _binding.Key = Keys.None;
            _binding.Ctrl = false;
            _binding.Alt = false;
            _binding.Shift = false;

            BindingChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return true;
        }

        if (keyCode == Keys.None)
            return true;

        _binding.Enabled = true;
        _binding.Ctrl = (keyData & Keys.Control) == Keys.Control;
        _binding.Alt = (keyData & Keys.Alt) == Keys.Alt;
        _binding.Shift = (keyData & Keys.Shift) == Keys.Shift;
        _binding.Key = keyCode;

        BindingChanged?.Invoke(this, EventArgs.Empty);

        _capturing = false;
        Parent?.Focus();
        Invalidate();

        return true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedGeometry.Path(rect, 12);

        Color fillColor = !Enabled
            ? AppTheme.Surface
            : _hovered || Focused
                ? AppTheme.SurfaceHover
                : AppTheme.SurfaceAlt;

        using var fill = new SolidBrush(fillColor);
        e.Graphics.FillPath(fill, path);

        using var pen = new Pen(
            Focused && _capturing ? AppTheme.Accent : AppTheme.Border,
            Focused && _capturing ? 2 : 1);

        e.Graphics.DrawPath(pen, path);

        string text;
        Color textColor;

        if (!Enabled)
        {
            text = "Skrót wyłączony";
            textColor = AppTheme.Muted;
        }
        else if (_capturing && Focused)
        {
            text = "Naciśnij kombinację…";
            textColor = AppTheme.Accent;
        }
        else if (_binding.Key == Keys.None)
        {
            text = "Kliknij i naciśnij skrót";
            textColor = AppTheme.Muted;
        }
        else
        {
            text = _binding.DisplayText;
            textColor = AppTheme.Text;
        }

        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            rect,
            textColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }
}


internal sealed class DarkMenuColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => AppTheme.Surface;
    public override Color ImageMarginGradientBegin => AppTheme.Surface;
    public override Color ImageMarginGradientMiddle => AppTheme.Surface;
    public override Color ImageMarginGradientEnd => AppTheme.Surface;
    public override Color MenuBorder => AppTheme.Border;
    public override Color MenuItemBorder => AppTheme.Border;
    public override Color MenuItemSelected => AppTheme.SurfaceHover;
    public override Color MenuItemSelectedGradientBegin => AppTheme.SurfaceHover;
    public override Color MenuItemSelectedGradientEnd => AppTheme.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => AppTheme.SurfaceHover;
    public override Color MenuItemPressedGradientEnd => AppTheme.SurfaceHover;
    public override Color SeparatorDark => AppTheme.Border;
    public override Color SeparatorLight => AppTheme.Border;
}

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer()
        : base(new DarkMenuColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled
            ? AppTheme.Text
            : AppTheme.Muted;

        base.OnRenderItemText(e);
    }
}
