using System.Text.Json;

namespace MiniWid.App.Services;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Rog20";
    public string Size { get; set; } = "Standard";
    public bool AlwaysOnTop { get; set; }
    public int PollIntervalSeconds { get; set; } = 30;
    public bool ShowThisPc { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public int WindowX { get; set; } = int.MinValue;
    public int WindowY { get; set; } = int.MinValue;
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public SettingsStore()
    {
        var portableDir = AppContext.BaseDirectory;
        var portablePath = Path.Combine(portableDir, "settings.json");
        var roamingDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniWid");
        var roamingPath = Path.Combine(roamingDir, "settings.json");

        if (IsWritable(portableDir))
        {
            if (!File.Exists(portablePath) && File.Exists(roamingPath))
            {
                try
                {
                    File.Copy(roamingPath, portablePath);
                }
                catch
                {
                    // Keep going with a fresh portable file.
                }
            }

            _path = portablePath;
            return;
        }

        Directory.CreateDirectory(roamingDir);
        _path = roamingPath;
    }

    private static bool IsWritable(string folder)
    {
        try
        {
            var probe = Path.Combine(folder, $".miniwid-write-{Guid.NewGuid():N}");
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
