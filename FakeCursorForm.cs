namespace MultiplePointers;

internal sealed class FakeCursorForm : Form
{
    private IntPtr _cursorHandle = IntPtr.Zero;
    private Point _hotspot = Point.Empty;

    public FakeCursorForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;

        BackColor = Color.FromArgb(1, 2, 3);
        TransparencyKey = BackColor;

        // Large enough for Windows accessibility cursor sizes.
        ClientSize = new Size(256, 256);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                        | NativeMethods.WS_EX_TRANSPARENT
                        | NativeMethods.WS_EX_TOOLWINDOW
                        | NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    /// <summary>
    /// Places the stationary normal-arrow cursor.
    ///
    /// Accessibility / no-flash rule:
    /// - never clear the old icon before the new icon is ready,
    /// - show and synchronously paint the new cursor before the caller
    ///   moves the real Windows cursor away.
    /// </summary>
    public bool ParkArrowAt(Point screenPoint)
    {
        IntPtr newHandle = NativeMethods.CopyIcon(Cursors.Arrow.Handle);

        if (newHandle == IntPtr.Zero)
            return false;

        Point newHotspot = Point.Empty;

        if (NativeMethods.GetIconInfo(newHandle, out var iconInfo))
        {
            newHotspot = new Point(
                (int)iconInfo.xHotspot,
                (int)iconInfo.yHotspot);

            if (iconInfo.hbmColor != IntPtr.Zero)
                NativeMethods.DeleteObject(iconInfo.hbmColor);

            if (iconInfo.hbmMask != IntPtr.Zero)
                NativeMethods.DeleteObject(iconInfo.hbmMask);
        }

        // Swap handles atomically from the point of view of OnPaint.
        // If a previous fake cursor is visible, it remains valid until the
        // replacement is already installed.
        IntPtr oldHandle = _cursorHandle;

        _cursorHandle = newHandle;
        _hotspot = newHotspot;

        Location = new Point(
            screenPoint.X - _hotspot.X,
            screenPoint.Y - _hotspot.Y);

        if (!Visible)
            Show();

        // Keep the overlay capturable by desktop capture.
        NativeMethods.SetWindowDisplayAffinity(
            Handle,
            NativeMethods.WDA_NONE);

        // IMPORTANT:
        // Do not leave the fake cursor repaint queued for "later".
        // The caller may immediately teleport the real cursor to another
        // monitor. Update() guarantees that the parked arrow has already
        // been painted before that handoff begins.
        Invalidate();
        Update();

        if (oldHandle != IntPtr.Zero)
            NativeMethods.DestroyIcon(oldHandle);

        return true;
    }

    /// <summary>
    /// Removes the fake arrow only after the real cursor has arrived at the
    /// same presentation position. Callers control that ordering.
    /// </summary>
    public void Unpark()
    {
        if (Visible)
            Hide();

        ReleaseCursor();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        IntPtr handle = _cursorHandle;

        if (handle == IntPtr.Zero)
            return;

        IntPtr hdc = e.Graphics.GetHdc();

        try
        {
            NativeMethods.DrawIconEx(
                hdc,
                0,
                0,
                handle,
                0,
                0,
                0,
                IntPtr.Zero,
                NativeMethods.DI_NORMAL);
        }
        finally
        {
            e.Graphics.ReleaseHdc(hdc);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_NCHITTEST)
        {
            m.Result = new IntPtr(NativeMethods.HTTRANSPARENT);
            return;
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseCursor();
        base.Dispose(disposing);
    }

    private void ReleaseCursor()
    {
        IntPtr handle = _cursorHandle;
        _cursorHandle = IntPtr.Zero;

        if (handle != IntPtr.Zero)
            NativeMethods.DestroyIcon(handle);
    }
}
