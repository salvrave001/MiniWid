namespace MiniWid.App.Services;

internal static class WidgetSize
{
    public const string Standard = "Standard";
    public const string Mini = "Mini";
    public const string Nano = "Nano";

    public static string Normalize(string? size) => size switch
    {
        Mini or Nano => size,
        _ => Standard
    };

    public static int Width(string? size, WidgetTheme? theme = null)
    {
        var baseWidth = Normalize(size) switch
        {
            Mini => 324,
            Nano => 288,
            _ => 408
        };

        var windowsTheme = theme?.Id is WidgetTheme.WindowsDark or WidgetTheme.WindowsLight;
        return baseWidth + (windowsTheme ? 48 : 0);
    }

    public static int WindowHeight(string? size, WidgetTheme theme, int deviceCount, bool isEmpty)
    {
        var normalized = Normalize(size);
        if (normalized == Standard)
        {
            return theme.WindowHeight(deviceCount, isEmpty);
        }

        if (isEmpty)
        {
            return normalized == Mini ? 112 : 88;
        }

        var rows = Math.Max(deviceCount, 1);
        if (normalized == Mini)
        {
            if (theme.BarStyle == "segments")
            {
                return 36 + (rows * 64);
            }

            var windows = theme.Id is WidgetTheme.WindowsDark or WidgetTheme.WindowsLight;
            var header = theme.ShowSlashes ? 56 : 28;
            var row = theme.ShowSlashes ? 62 : windows ? 70 : 60;
            return header + (rows * row) + (windows ? 10 : 12);
        }

        var nanoHeader = 22;
        return nanoHeader + (rows * 28) + 6;
    }
}
