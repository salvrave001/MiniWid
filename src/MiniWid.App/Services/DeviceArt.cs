using Microsoft.UI.Xaml.Media.Imaging;
using MiniWid.Core.Models;

namespace MiniWid.App.Services;

internal static class DeviceArt
{
    public static BitmapImage? Resolve(string name, DeviceKind kind)
    {
        var file = FileName(name, kind);
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Devices", file);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var image = new BitmapImage();
            image.SetSource(stream.AsRandomAccessStream());
            return image;
        }
        catch
        {
            return null;
        }
    }

    public static (double Scale, double OffsetX) Layout(string name, DeviceKind kind)
    {
        var haystack = name.ToLowerInvariant();
        if (IsSonyHeadphones(haystack))
        {
            return (1.10, 0);
        }

        if (IsRogHeadphones(haystack, kind))
        {
            return (1.22, -10);
        }

        if (IsManba(haystack))
        {
            return (0.88, 0);
        }

        if (IsWlMouse(haystack))
        {
            return (1.14, 0);
        }

        return kind switch
        {
            DeviceKind.Headphones => (1.05, 0),
            DeviceKind.Mouse => (1.06, 0),
            DeviceKind.Keyboard => (1.08, 0),
            DeviceKind.Gamepad => (0.96, 0),
            DeviceKind.Laptop => (0.98, 0),
            _ => (1.02, 0)
        };
    }

    private static string FileName(string name, DeviceKind kind)
    {
        var haystack = name.ToLowerInvariant();
        if (IsSonyHeadphones(haystack))
        {
            return "sony-wh-1000xm4.png";
        }

        if (IsRogHeadphones(haystack, kind))
        {
            return "rog-delta-ii.png";
        }

        if (IsManba(haystack))
        {
            return "manba-one-v2.png";
        }

        if (IsWlMouse(haystack))
        {
            return "beast-x-max.png";
        }

        return kind switch
        {
            DeviceKind.Headphones => "default-headphones.png",
            DeviceKind.Keyboard => "default-keyboard.png",
            DeviceKind.Mouse => "default-mouse.png",
            DeviceKind.Gamepad => "default-gamepad.png",
            DeviceKind.Laptop => "default-laptop.png",
            _ => "default-other.png"
        };
    }

    internal static bool IsSonyHeadphones(string name)
    {
        var haystack = name.ToLowerInvariant();
        return haystack.Contains("wh-1000")
            || haystack.Contains("wh1000")
            || haystack.Contains("1000xm")
            || haystack.Contains("xm4")
            || haystack.Contains("xm5")
            || haystack.Contains("sony");
    }

    private static bool IsRogHeadphones(string haystack, DeviceKind kind) =>
        haystack.Contains("delta") || (haystack.Contains("rog") && kind == DeviceKind.Headphones);

    private static bool IsManba(string haystack) =>
        haystack.Contains("manba") || haystack.Contains("one v2");

    private static bool IsWlMouse(string haystack) =>
        haystack.Contains("beast") || haystack.Contains("wlmouse");
}
