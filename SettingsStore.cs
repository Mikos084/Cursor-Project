using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiplePointers;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SettingsPath
        => Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static string BackupPath
        => SettingsPath + ".bak";

    public static AppSettings Load()
    {
        AppSettings? primary = TryLoad(SettingsPath);
        if (primary is not null)
            return primary;

        // If the last save was interrupted, try the previous known-good file.
        AppSettings? backup = TryLoad(BackupPath);
        return backup ?? AppSettings.Defaults();
    }

    public static bool Save(AppSettings settings, out string? error)
    {
        error = null;
        string path = SettingsPath;
        string temp = path + ".tmp";
        string backup = BackupPath;

        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);

            // Write to a separate file first. The final settings file remains
            // untouched until serialization/write has completed successfully.
            File.WriteAllText(temp, json, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                // Atomic replacement on Windows, while retaining one backup.
                File.Replace(temp, path, backup, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temp, path);
            }

            return true;
        }
        catch (Exception ex)
        {
            TryDelete(temp);
            error = "Nie udało się zapisać settings.json: " + ex.Message;
            return false;
        }
    }

    private static AppSettings? TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is not null)
                settings.DisplayName = AppBranding.TryValidate(settings.DisplayName, out var name, out _)
                    ? name : AppBranding.DefaultName;
            return settings;
        }
        catch
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cleanup failure must not hide the original save error.
        }
    }
}
