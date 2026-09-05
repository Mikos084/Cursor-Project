using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MultiplePointers;

internal sealed class MonitorDescriptor
{
    public required string DeviceName { get; init; }
    public required string FriendlyName { get; init; }
    public required string DeviceId { get; init; }
    public required Screen Screen { get; init; }

    public int DisplayNumber { get; init; }
    public string PositionHint { get; init; } = "";

    public Rectangle Bounds => Screen.Bounds;
    public Rectangle WorkingArea => Screen.WorkingArea;
    public bool Primary => Screen.Primary;

    public string ShortName
        => $"Monitor {DisplayNumber}" +
           (Primary ? " (główny)" : "");

    public string FullLabel
    {
        get
        {
            string position = string.IsNullOrWhiteSpace(PositionHint)
                ? ""
                : $" • {PositionHint}";

            return
                $"{ShortName} — {FriendlyName} — " +
                $"{Bounds.Width}×{Bounds.Height}" +
                $"{position} • X={Bounds.Left}, Y={Bounds.Top}";
        }
    }
}

internal static class MonitorService
{
    private static readonly Regex DisplayNumberRegex =
        new(@"DISPLAY(?<n>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<MonitorDescriptor> GetMonitors()
    {
        Screen[] screens = Screen.AllScreens;

        if (screens.Length == 0)
            return Array.Empty<MonitorDescriptor>();

        Screen primary = screens.FirstOrDefault(s => s.Primary) ?? screens[0];

        var result = new List<MonitorDescriptor>();

        foreach (Screen screen in screens)
        {
            var device = ReadPhysicalMonitorInfo(screen.DeviceName);

            int number = ParseDisplayNumber(screen.DeviceName);

            if (number <= 0)
            {
                number = result.Count + 1;
            }

            result.Add(new MonitorDescriptor
            {
                DeviceName = screen.DeviceName,
                FriendlyName = CleanFriendlyName(device.friendlyName),
                DeviceId = device.deviceId,
                Screen = screen,
                DisplayNumber = number,
                PositionHint = DescribePosition(screen, primary)
            });
        }

        return result
            .OrderBy(m => m.DisplayNumber)
            .ThenBy(m => m.Bounds.Left)
            .ThenBy(m => m.Bounds.Top)
            .ToArray();
    }

    public static MonitorDescriptor? Find(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return null;

        return GetMonitors().FirstOrDefault(
            m => string.Equals(
                m.DeviceName,
                deviceName,
                StringComparison.OrdinalIgnoreCase));
    }

    public static MonitorDescriptor? FromPoint(Point point)
    {
        Screen screen = Screen.FromPoint(point);

        return Find(screen.DeviceName);
    }

    private static (string friendlyName, string deviceId)
        ReadPhysicalMonitorInfo(string gdiDeviceName)
    {
        string bestName = "";
        string bestId = "";

        // Microsoft documents that after querying the display adapter,
        // querying EnumDisplayDevices again with its GDI name returns
        // monitor information in DeviceString.
        for (uint index = 0; index < 16; index++)
        {
            var monitor = new NativeMethods.DISPLAY_DEVICE
            {
                cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>()
            };

            bool ok = NativeMethods.EnumDisplayDevices(
                gdiDeviceName,
                index,
                ref monitor,
                NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME);

            if (!ok)
                break;

            string name = monitor.DeviceString?.Trim() ?? "";
            string id = monitor.DeviceID?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(bestName) &&
                !string.IsNullOrWhiteSpace(name))
            {
                bestName = name;
            }

            if (string.IsNullOrWhiteSpace(bestId) &&
                !string.IsNullOrWhiteSpace(id))
            {
                bestId = id;
            }

            if (!string.IsNullOrWhiteSpace(bestName) &&
                !string.IsNullOrWhiteSpace(bestId))
            {
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(bestName))
            bestName = gdiDeviceName;

        return (bestName, bestId);
    }

    private static int ParseDisplayNumber(string deviceName)
    {
        Match match = DisplayNumberRegex.Match(deviceName);

        return match.Success &&
               int.TryParse(match.Groups["n"].Value, out int number)
            ? number
            : 0;
    }

    private static string DescribePosition(Screen screen, Screen primary)
    {
        if (screen.Primary)
            return "ekran główny Windows";

        Point a = Center(screen.Bounds);
        Point b = Center(primary.Bounds);

        int dx = a.X - b.X;
        int dy = a.Y - b.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx >= 0 ? "po prawej" : "po lewej";

        return dy >= 0 ? "poniżej" : "powyżej";
    }

    private static Point Center(Rectangle r)
        => new(r.Left + r.Width / 2, r.Top + r.Height / 2);

    private static string CleanFriendlyName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Nieznany monitor";

        string v = value.Trim();

        // Still useful, but slightly clearer to users than an empty label.
        if (v.Equals(
            "Generic PnP Monitor",
            StringComparison.OrdinalIgnoreCase))
        {
            return "Monitor Plug and Play";
        }

        return v;
    }
}
