using System.Runtime.InteropServices;

namespace MultiplePointers;

internal sealed class PointerController : IDisposable
{
    private readonly FakeCursorForm _fakeCursor = new();

    private Point? _savedPresentationPosition;
    private Point? _savedControlPosition;

    private bool _controllingPresentation;

    public string? ParkedScreenName { get; private set; }
    public string? LockedScreenName { get; private set; }

    public string? PresentationScreenName { get; private set; }
    public string? ControlScreenName { get; private set; }

    public bool IsParked => _fakeCursor.Visible;
    public bool IsLocked => LockedScreenName is not null;

    public bool HasSavedPresentationPosition
        => _savedPresentationPosition.HasValue;

    public bool HasSavedControlPosition
        => _savedControlPosition.HasValue;

    public Point? SavedPresentationPosition
        => _savedPresentationPosition;

    public Point? SavedControlPosition
        => _savedControlPosition;

    public bool IsControllingPresentation
        => _controllingPresentation;

    public bool IsSessionActive
        => HasSavedPresentationPosition ||
           HasSavedControlPosition ||
           IsParked ||
           IsLocked;

    public event Action? StateChanged;

    public string CurrentScreenName
        => Screen.FromPoint(Cursor.Position).DeviceName;

    public Point CurrentCursorPosition
        => Cursor.Position;

    public int ScreenCount
        => Screen.AllScreens.Length;

    public PointerController()
    {
        AutoSelectScreens();
    }

    public IReadOnlyList<MonitorDescriptor> GetMonitors()
        => MonitorService.GetMonitors();

    public void RefreshMonitors()
    {
        var monitors = GetMonitors();

        if (monitors.Count == 0)
            return;

        bool presentationStillExists =
            monitors.Any(m => SameDevice(m.DeviceName, PresentationScreenName));

        bool controlStillExists =
            monitors.Any(m => SameDevice(m.DeviceName, ControlScreenName));

        if (!presentationStillExists || !controlStillExists)
        {
            // A display disappeared or its GDI identity changed. Release any
            // global cursor restriction before choosing a new topology.
            if (IsSessionActive)
                Reset();

            AutoSelectScreens();
        }
        else
        {
            RaiseStateChanged();
        }
    }

    public void AutoSelectScreens()
    {
        var monitors = GetMonitors();

        if (monitors.Count == 0)
            return;

        var primary = monitors.FirstOrDefault(m => m.Primary) ?? monitors[0];

        PresentationScreenName = primary.DeviceName;

        var other = monitors.FirstOrDefault(
            m => !SameDevice(m.DeviceName, primary.DeviceName));

        ControlScreenName = other?.DeviceName ?? primary.DeviceName;

        RaiseStateChanged();
    }

    public void SetPresentationScreen(string deviceName)
    {
        if (MonitorService.Find(deviceName) is null)
            return;

        if (!SameDevice(PresentationScreenName, deviceName))
            _savedPresentationPosition = null;

        PresentationScreenName = deviceName;

        if (SameDevice(PresentationScreenName, ControlScreenName))
        {
            var other = GetMonitors().FirstOrDefault(
                m => !SameDevice(m.DeviceName, deviceName));

            if (other is not null)
            {
                if (!SameDevice(ControlScreenName, other.DeviceName))
                    _savedControlPosition = null;

                ControlScreenName = other.DeviceName;
            }
        }

        RaiseStateChanged();
    }

    public void SetControlScreen(string deviceName)
    {
        if (MonitorService.Find(deviceName) is null)
            return;

        if (!SameDevice(ControlScreenName, deviceName))
            _savedControlPosition = null;

        ControlScreenName = deviceName;

        if (SameDevice(PresentationScreenName, ControlScreenName))
        {
            var other = GetMonitors().FirstOrDefault(
                m => !SameDevice(m.DeviceName, deviceName));

            if (other is not null)
            {
                if (!SameDevice(PresentationScreenName, other.DeviceName))
                    _savedPresentationPosition = null;

                PresentationScreenName = other.DeviceName;
            }
        }

        RaiseStateChanged();
    }

    public bool TogglePark(out string? error)
    {
        error = null;

        if (IsParked)
        {
            _fakeCursor.Unpark();
            RaiseStateChanged();
            return true;
        }

        return ParkHere(out error);
    }

    public bool ParkHere(out string? error)
    {
        error = null;

        var presentation = MonitorService.Find(PresentationScreenName);

        if (presentation is null)
        {
            error = "Nie znaleziono wybranego ekranu udostępnianego. Kliknij „Odśwież monitory”.";
            return false;
        }

        Point position = Cursor.Position;
        Screen actualScreen = Screen.FromPoint(position);

        if (!SameDevice(actualScreen.DeviceName, presentation.DeviceName))
        {
            var actual = MonitorService.Find(actualScreen.DeviceName);

            error =
                $"Kursor jest teraz na: {actual?.ShortName ?? actualScreen.DeviceName}.\n\n" +
                $"Przenieś go na: {presentation.ShortName} — {presentation.FriendlyName}, " +
                "ustaw w miejscu, w którym ma zostać strzałka, i spróbuj ponownie.";
            return false;
        }

        if (!presentation.Bounds.Contains(position))
        {
            error = "Pozycja kursora nie mieści się w granicach wybranego monitora.";
            return false;
        }

        if (!_fakeCursor.ParkArrowAt(position))
        {
            error = "Nie udało się utworzyć nieruchomej strzałki.";
            return false;
        }

        _savedPresentationPosition = position;
        ParkedScreenName = presentation.DeviceName;
        _controllingPresentation = false;

        RaiseStateChanged();
        return true;
    }

    public bool ParkAndGoToControlScreen(out string? error)
    {
        error = null;

        if (Screen.AllScreens.Length < 2)
        {
            error = "Tryb prezentacji wymaga co najmniej dwóch aktywnych monitorów.";
            return false;
        }

        var presentation = MonitorService.Find(PresentationScreenName);
        var control = MonitorService.Find(ControlScreenName);

        if (presentation is null || control is null)
        {
            error = "Nie udało się odnaleźć wybranych monitorów. Kliknij „Odśwież monitory” i wybierz je ponownie.";
            return false;
        }

        if (SameDevice(presentation.DeviceName, control.DeviceName))
        {
            error = "Ekran udostępniany i ekran prywatny muszą być różnymi monitorami.";
            return false;
        }

        if (!ParkHere(out error))
            return false;

        return MoveAndLockToMonitor(
            control,
            _savedControlPosition,
            controllingPresentation: false,
            out error);
    }

    public bool TogglePresentationControl(out string? error)
    {
        error = null;

        var presentation = MonitorService.Find(PresentationScreenName);
        var control = MonitorService.Find(ControlScreenName);

        if (presentation is null || control is null)
        {
            error = "Nie znaleziono wybranych monitorów. Odśwież listę monitorów.";
            return false;
        }

        if (!_savedPresentationPosition.HasValue)
        {
            error = "Najpierw uruchom prezentację, aby zapisać pozycję kursora.";
            return false;
        }

        if (!_controllingPresentation)
        {
            // LEAVING PRIVATE SCREEN:
            // remember exactly where the real cursor was left.
            Point currentPrivate = Cursor.Position;
            Screen actualPrivate = Screen.FromPoint(currentPrivate);

            if (!SameDevice(actualPrivate.DeviceName, control.DeviceName))
            {
                error =
                    $"Przed przełączeniem kursor powinien znajdować się na " +
                    $"{control.ShortName} ({control.FriendlyName}).";
                return false;
            }

            _savedControlPosition = ClampPointToBounds(
                currentPrivate,
                control.Bounds);

            Point presentationTarget = ClampPointToBounds(
                _savedPresentationPosition.Value,
                presentation.Bounds);

            // ACCESSIBILITY / NO-FLASH HANDOFF:
            //
            // RC 0.8.1 removed the fake arrow BEFORE SetCursorPos moved the
            // real cursor back to the presentation. That could produce a
            // short blank frame and look like a cursor flash.
            //
            // Keep the fake arrow visible while the real cursor is moved
            // exactly underneath it. Only after Windows confirms that the
            // real cursor arrived do we remove the fake one.
            bool moved = MoveAndLockToMonitor(
                presentation,
                presentationTarget,
                controllingPresentation: true,
                out error);

            if (!moved)
                return false;

            _fakeCursor.Unpark();
            ParkedScreenName = null;

            RaiseStateChanged();
            return true;
        }

        // LEAVING PRESENTATION SCREEN:
        // remember exactly where the real cursor was left.
        Point currentPresentation = Cursor.Position;
        Screen actualPresentation = Screen.FromPoint(currentPresentation);

        if (!SameDevice(actualPresentation.DeviceName, presentation.DeviceName))
        {
            error =
                $"Prawdziwy kursor powinien znajdować się na " +
                $"{presentation.ShortName} ({presentation.FriendlyName}).";
            return false;
        }

        _savedPresentationPosition = ClampPointToBounds(
            currentPresentation,
            presentation.Bounds);

        if (!_fakeCursor.ParkArrowAt(_savedPresentationPosition.Value))
        {
            error = "Nie udało się ponownie narysować nieruchomej strzałki.";
            return false;
        }

        ParkedScreenName = presentation.DeviceName;

        // Return to the private monitor's OWN last position.
        return MoveAndLockToMonitor(
            control,
            _savedControlPosition,
            controllingPresentation: false,
            out error);
    }

    public bool GoToControlScreen(out string? error)
    {
        error = null;

        var control = MonitorService.Find(ControlScreenName);

        if (control is null)
        {
            error = "Nie znaleziono wybranego ekranu prywatnego.";
            return false;
        }

        return MoveAndLockToMonitor(
            control,
            _savedControlPosition,
            controllingPresentation: false,
            out error);
    }

    public bool GoToPresentationSavedPosition(out string? error)
    {
        error = null;

        if (!_savedPresentationPosition.HasValue)
        {
            error = "Nie ma jeszcze zapisanej pozycji kursora. Najpierw uruchom prezentację.";
            return false;
        }

        if (_controllingPresentation)
            return true;

        return TogglePresentationControl(out error);
    }

    public void Unlock()
    {
        NativeMethods.ClipCursor(IntPtr.Zero);
        LockedScreenName = null;
        RaiseStateChanged();
    }

    public void Unpark()
    {
        _fakeCursor.Unpark();
        RaiseStateChanged();
    }

    public void Reset()
    {
        _fakeCursor.Unpark();
        NativeMethods.ClipCursor(IntPtr.Zero);

        ParkedScreenName = null;
        LockedScreenName = null;

        _savedPresentationPosition = null;
        _savedControlPosition = null;

        _controllingPresentation = false;

        RaiseStateChanged();
    }

    public void Dispose()
    {
        Reset();
        _fakeCursor.Dispose();
    }

    private bool MoveAndLockToMonitor(
        MonitorDescriptor target,
        Point? rememberedPosition,
        bool controllingPresentation,
        out string? error)
    {
        error = null;

        if (!ReleaseClip(out error))
            return false;

        Point targetPoint;

        if (rememberedPosition.HasValue)
        {
            targetPoint = ClampPointToBounds(
                rememberedPosition.Value,
                target.Bounds);
        }
        else
        {
            // Only a first-use fallback. Once a screen has been left once,
            // its own X/Y will be restored instead.
            targetPoint = new Point(
                target.WorkingArea.Left + target.WorkingArea.Width / 2,
                target.WorkingArea.Top + target.WorkingArea.Height / 2);
        }

        if (!NativeMethods.SetCursorPos(targetPoint.X, targetPoint.Y))
        {
            int code = Marshal.GetLastWin32Error();

            error =
                $"Windows nie pozwolił przenieść kursora na {target.ShortName}. " +
                $"Kod błędu: {code}.";
            return false;
        }

        Screen actual = Screen.FromPoint(Cursor.Position);

        if (!SameDevice(actual.DeviceName, target.DeviceName))
        {
            Point fallback = new(
                target.Bounds.Left + target.Bounds.Width / 2,
                target.Bounds.Top + target.Bounds.Height / 2);

            NativeMethods.SetCursorPos(fallback.X, fallback.Y);
            actual = Screen.FromPoint(Cursor.Position);
        }

        if (!SameDevice(actual.DeviceName, target.DeviceName))
        {
            error =
                $"Kursor nie trafił na {target.ShortName} ({target.FriendlyName}). " +
                "Odśwież monitory i sprawdź ich wybór.";
            return false;
        }

        if (!ApplyClip(target, out error))
            return false;

        LockedScreenName = target.DeviceName;
        _controllingPresentation = controllingPresentation;

        RaiseStateChanged();
        return true;
    }

    private static bool ApplyClip(
        MonitorDescriptor target,
        out string? error)
    {
        error = null;

        var rect = new NativeMethods.RECT(target.Bounds);

        if (NativeMethods.ClipCursor(ref rect))
            return true;

        int code = Marshal.GetLastWin32Error();

        error =
            $"Nie udało się ograniczyć kursora do {target.ShortName} " +
            $"({target.FriendlyName}). Kod Windows: {code}.";

        return false;
    }

    private static bool ReleaseClip(out string? error)
    {
        error = null;

        if (NativeMethods.ClipCursor(IntPtr.Zero))
            return true;

        int code = Marshal.GetLastWin32Error();

        error =
            $"Nie udało się zwolnić poprzedniej blokady kursora. Kod Windows: {code}.";

        return false;
    }

    private static Point ClampPointToBounds(Point point, Rectangle bounds)
    {
        return new Point(
            Math.Clamp(point.X, bounds.Left + 1, bounds.Right - 2),
            Math.Clamp(point.Y, bounds.Top + 1, bounds.Bottom - 2));
    }

    private static bool SameDevice(string? a, string? b)
        => string.Equals(
            a,
            b,
            StringComparison.OrdinalIgnoreCase);

    private void RaiseStateChanged()
        => StateChanged?.Invoke();
}
