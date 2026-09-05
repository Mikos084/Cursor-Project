namespace MultiplePointers;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    public event Action<int>? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "MultiplePointers.HotkeyWindow",
            X = 0,
            Y = 0,
            Height = 0,
            Width = 0,
            Style = 0
        });
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
            HotkeyPressed?.Invoke(m.WParam.ToInt32());

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}
