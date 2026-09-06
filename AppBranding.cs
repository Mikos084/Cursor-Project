using System.Globalization;
using System.Text;

namespace MultiplePointers;

internal static class AppBranding
{
    public const string DefaultName = "Multiple Pointers";
    public const int MaxNameLength = 40;
    public static string Name { get; set; } = DefaultName;
    public static string DisplayName => Format(Name);

    public static string Format(string name)
        => name;

    public static bool TryValidate(string? input, out string name, out string? error)
    {
        name = input?.Trim() ?? "";
        error = null;
        if (name.Length is < 1 or > MaxNameLength ||
            name.Any(c => !(char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')))
        {
            error = "Wpisz 1–40 znaków: litery, cyfry, spacje, myślnik lub podkreślenie.";
            return false;
        }

        string key = new string(name.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark &&
                        char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
        string[] reserved = ["windows", "microsoft", "system", "svchost", "explorer",
            "taskmgr", "taskmanager", "menedzerzadan", "defender", "securityhealth",
            "lsass", "csrss", "winlogon", "services", "rundll32", "dwm", "smss",
            "notepad", "notatnik", "kalkulator", "calculator", "controlpanel", "panelsterowania"];
        if (reserved.Any(key.Contains))
        {
            error = "Wybierz własną nazwę, która nie sugeruje aplikacji systemowej.";
            return false;
        }
        return true;
    }
}
