using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MiniWid.App.Services;

internal sealed class WidgetTheme
{
    public const string Rog = "Rog";
    public const string Rog20 = "Rog20";
    public const string Cyberpunk = "Cyberpunk";
    public const string WindowsDark = "WindowsDark";
    public const string WindowsLight = "WindowsLight";

    public required string Id { get; init; }
    public required ElementTheme ElementTheme { get; init; }
    public required bool ShowSlashes { get; init; }
    public required bool ShowEdition20 { get; init; }
    public required bool ShowBrandMark { get; init; }
    public required string HeaderBadge { get; init; }
    public required bool TitleAllCaps { get; init; }
    public required bool UseCondensedFont { get; init; }
    public required int TitleSpacing { get; init; }
    public required double TitleSize { get; init; }
    public required double RowCorner { get; init; }
    public required double BatteryCorner { get; init; }
    public required double SlashOpacity { get; init; }
    public required double RowEdgeOpacity { get; init; }
    public required Thickness ContentMargin { get; init; }
    public required int HeaderExtra { get; init; }
    public required int RowHeight { get; init; }

    public required string Card { get; init; }
    public required string Primary { get; init; }
    public required string Secondary { get; init; }
    public required string Row { get; init; }
    public required string RowBorder { get; init; }
    public required string Accent { get; init; }
    public required string AccentStrong { get; init; }
    public required string Menu { get; init; }
    public required string Border { get; init; }
    public required string Slash { get; init; }
    public required string RowEdge { get; init; }
    public required string FillHealthy { get; init; }
    public required string FillWarn { get; init; }
    public required string FillLow { get; init; }

    public FontFamily Font => Id == Cyberpunk
        ? (FontFamily)Application.Current.Resources["CyberpunkHudFont"]
        : UseCondensedFont
            ? (FontFamily)Application.Current.Resources["RogTitleFont"]
            : new FontFamily("Segoe UI Variable");

    public static string Normalize(string? theme) => theme switch
    {
        Rog or Rog20 or Cyberpunk or WindowsDark or WindowsLight => theme,
        "Light" => WindowsLight,
        "Dark" or "System" => Rog20,
        _ => Rog20
    };

    public static WidgetTheme From(string? theme) => Normalize(theme) switch
    {
        Rog => ClassicRog(),
        Cyberpunk => NightCity(),
        WindowsDark => FluentDark(),
        WindowsLight => FluentLight(),
        _ => Anniversary()
    };

    public int WindowHeight(int deviceCount, bool isEmpty)
    {
        var rows = Math.Max(deviceCount, isEmpty ? 2 : 1);
        return HeaderExtra + (rows * RowHeight) + 28;
    }

    public static Color Parse(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }

        return Color.FromArgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16),
            Convert.ToByte(hex[6..8], 16));
    }

    public static SolidColorBrush Brush(string hex) => new(Parse(hex));

    private static WidgetTheme ClassicRog() => new()
    {
        Id = Rog,
        ElementTheme = ElementTheme.Dark,
        ShowSlashes = true,
        ShowEdition20 = false,
        ShowBrandMark = true,
        HeaderBadge = "",
        TitleAllCaps = true,
        UseCondensedFont = true,
        TitleSpacing = 90,
        TitleSize = 14,
        RowCorner = 0,
        BatteryCorner = 0,
        SlashOpacity = 1,
        RowEdgeOpacity = 1,
        ContentMargin = new Thickness(1, 9, 8, 9),
        HeaderExtra = 82,
        RowHeight = 104,
        Card = "#101010",
        Primary = "#F2F2F2",
        Secondary = "#B3B3B3",
        Row = "#181818",
        RowBorder = "#333333",
        Accent = "#FF1929",
        AccentStrong = "#FF1929",
        Menu = "#FF1929",
        Border = "#991019",
        Slash = "#FF1929",
        RowEdge = "#FF1929",
        FillHealthy = "#FF1929",
        FillWarn = "#FF8A00",
        FillLow = "#FF1929"
    };

    private static WidgetTheme Anniversary() => new()
    {
        Id = Rog20,
        ElementTheme = ElementTheme.Dark,
        ShowSlashes = true,
        ShowEdition20 = true,
        ShowBrandMark = false,
        HeaderBadge = "",
        TitleAllCaps = true,
        UseCondensedFont = true,
        TitleSpacing = 90,
        TitleSize = 14,
        RowCorner = 0,
        BatteryCorner = 0,
        SlashOpacity = 1,
        RowEdgeOpacity = 1,
        ContentMargin = new Thickness(1, 9, 8, 9),
        HeaderExtra = 82,
        RowHeight = 104,
        Card = "#101010",
        Primary = "#F2F2F2",
        Secondary = "#B3B3B3",
        Row = "#181818",
        RowBorder = "#333333",
        Accent = "#E0BE62",
        AccentStrong = "#EEBE38",
        Menu = "#E0BE62",
        Border = "#AE934B",
        Slash = "#E0BE62",
        RowEdge = "#EEBE38",
        FillHealthy = "#E0BE62",
        FillWarn = "#EEBE38",
        FillLow = "#FF1929"
    };

    private static WidgetTheme FluentDark() => new()
    {
        Id = WindowsDark,
        ElementTheme = ElementTheme.Dark,
        ShowSlashes = false,
        ShowEdition20 = false,
        ShowBrandMark = false,
        HeaderBadge = "",
        TitleAllCaps = false,
        UseCondensedFont = false,
        TitleSpacing = 0,
        TitleSize = 15,
        RowCorner = 8,
        BatteryCorner = 2,
        SlashOpacity = 0,
        RowEdgeOpacity = 0,
        ContentMargin = new Thickness(10, 8, 14, 10),
        HeaderExtra = 62,
        RowHeight = 98,
        Card = "#202020",
        Primary = "#FFFFFF",
        Secondary = "#9A9A9A",
        Row = "#2C2C2C",
        RowBorder = "#3F3F3F",
        Accent = "#4CC2FF",
        AccentStrong = "#FFFFFF",
        Menu = "#FFFFFF",
        Border = "#3F3F3F",
        Slash = "#202020",
        RowEdge = "#2C2C2C",
        FillHealthy = "#6CCB5F",
        FillWarn = "#FCE100",
        FillLow = "#FF99A4"
    };

    private static WidgetTheme FluentLight() => new()
    {
        Id = WindowsLight,
        ElementTheme = ElementTheme.Light,
        ShowSlashes = false,
        ShowEdition20 = false,
        ShowBrandMark = false,
        HeaderBadge = "",
        TitleAllCaps = false,
        UseCondensedFont = false,
        TitleSpacing = 0,
        TitleSize = 15,
        RowCorner = 8,
        BatteryCorner = 2,
        SlashOpacity = 0,
        RowEdgeOpacity = 0,
        ContentMargin = new Thickness(10, 8, 14, 10),
        HeaderExtra = 62,
        RowHeight = 98,
        Card = "#F3F3F3",
        Primary = "#1A1A1A",
        Secondary = "#5D5D5D",
        Row = "#FFFFFF",
        RowBorder = "#E5E5E5",
        Accent = "#005FB8",
        AccentStrong = "#1A1A1A",
        Menu = "#1A1A1A",
        Border = "#E5E5E5",
        Slash = "#F3F3F3",
        RowEdge = "#FFFFFF",
        FillHealthy = "#0F7B0F",
        FillWarn = "#9D5D00",
        FillLow = "#C42B1C"
    };

    private static WidgetTheme NightCity() => new()
    {
        Id = Cyberpunk,
        ElementTheme = ElementTheme.Dark,
        ShowSlashes = false,
        ShowEdition20 = false,
        ShowBrandMark = false,
        HeaderBadge = "2077",
        TitleAllCaps = true,
        UseCondensedFont = true,
        TitleSpacing = 90,
        TitleSize = 15,
        RowCorner = 0,
        BatteryCorner = 0,
        SlashOpacity = 0,
        RowEdgeOpacity = 1,
        ContentMargin = new Thickness(10, 20, 10, 18),
        HeaderExtra = 96,
        RowHeight = 104,
        Card = "#10080A",
        Primary = "#E85D52",
        Secondary = "#9A4A44",
        Row = "#160C0E",
        RowBorder = "#5A2A28",
        Accent = "#3DEFE3",
        AccentStrong = "#6FFFF6",
        Menu = "#3DEFE3",
        Border = "#C45A4C",
        Slash = "#E85D52",
        RowEdge = "#E85D52",
        FillHealthy = "#3DEFE3",
        FillWarn = "#F0A040",
        FillLow = "#E85D52"
    };
}
