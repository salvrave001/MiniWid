using MiniWid.Core.Models;

namespace MiniWid.Core.Services;

public static class DeviceKindClassifier
{
    public static DeviceKind Classify(string name, string? deviceClass = null)
    {
        var haystack = $"{name} {deviceClass}".ToLowerInvariant();

        if (ContainsAny(haystack, "headphone", "headset", "earbuds", "earbud", "airpods", "наушник", "гарнитур")
            || ContainsAny(haystack, "wh-", "wf-", "1000xm", "xm4", "xm5", "xm3", "delta")
            || IsClass(deviceClass, "MEDIA", "AudioEndpoint", "Headset"))
        {
            return DeviceKind.Headphones;
        }

        if (ContainsAny(haystack, "keyboard", "клавиатур")
            || IsClass(deviceClass, "Keyboard"))
        {
            return DeviceKind.Keyboard;
        }

        if (ContainsAny(haystack, "mouse", "мышь", "мыши", "мыша", "wlmouse", "beast x")
            || IsClass(deviceClass, "Mouse"))
        {
            return DeviceKind.Mouse;
        }

        if (ContainsAny(haystack, "gamepad", "joystick", "xinput", "xbox", "manba", "геймпад")
            || (ContainsAny(haystack, "controller", "контроллер")
                && !ContainsAny(haystack, "keyboard", "клавиатур", "led", "aura", "usb audio")))
        {
            return DeviceKind.Gamepad;
        }

        if (ContainsAny(haystack, "laptop", "notebook", "ноутбук"))
        {
            return DeviceKind.Laptop;
        }

        return DeviceKind.Other;
    }

    public static string CleanName(string name)
    {
        string[] suffixes =
        [
            " Hands-Free AG",
            " Hands-Free",
            " Handsfree",
            " Avrcp Transport",
            " AVRCP Transport",
            " Stereo",
            " (2.4GHz)",
            " (2.4 GHz)",
        ];

        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^suffix.Length].Trim();
            }
        }

        return name;
    }

    public static bool IsNoiseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        string[] noise =
        [
            "microsoft bluetooth enumerator",
            "bluetooth device (rfcomm",
            "bluetooth le enumerator",
            "generic attribute",
            "generic access",
            "microsoft radio",
            "intel(r) wireless",
            "realtek bluetooth",
            "qualcomm",
            "transport"
        ];

        var lower = name.ToLowerInvariant();
        return noise.Any(n => lower.Contains(n));
    }

    private static bool IsClass(string? deviceClass, params string[] names) =>
        !string.IsNullOrWhiteSpace(deviceClass)
        && names.Any(n => deviceClass.Equals(n, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(haystack.Contains);
}
