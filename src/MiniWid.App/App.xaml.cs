using Microsoft.UI.Xaml;

namespace MiniWid.App;

public partial class App : Application
{
    private const string MutexName = @"Local\MiniWid.BatteryWidget";
    private Mutex? _mutex;
    private MainWindow? _window;

    public App()
    {
        UnhandledException += (_, e) =>
        {
            CrashLog.Write(e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                CrashLog.Write(ex);
            }
        };
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out var createdNew);
            if (!createdNew)
            {
                Environment.Exit(0);
                return;
            }

            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            throw;
        }
    }
}

internal static class CrashLog
{
    public static void Write(Exception ex)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiniWid");
            Directory.CreateDirectory(folder);
            File.AppendAllText(
                Path.Combine(folder, "crash.log"),
                $"[{DateTime.Now:O}] {ex}{Environment.NewLine}{ex.InnerException}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures.
        }
    }
}
