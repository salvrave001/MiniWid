using System.Runtime.InteropServices;
using MiniWid.Core.Models;
using Windows.Devices.Power;
using Windows.Gaming.Input;
using Windows.System.Power;

namespace MiniWid.Core.Services;

public sealed class GamepadBatterySource : IBatterySource
{
    private const ushort Xbox360VendorId = 0x045E;
    private const ushort Xbox360ProductId = 0x028E;

    public Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vendor = ManbaVendorHid.TryRead(cancellationToken);
        var results = new List<AccessoryBattery>();

        foreach (var pad in RawGameController.RawGameControllers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = FriendlyName(pad.DisplayName, pad.HardwareVendorId, pad.HardwareProductId);
            var report = pad.TryGetBatteryReport();
            var percent = PercentFromReport(report) ?? vendor?.Percent;
            var charging = IsReallyCharging(report) || (vendor?.IsCharging ?? false);
            results.Add(new AccessoryBattery(
                Id: $"gamepad:{pad.HardwareVendorId:X4}:{pad.HardwareProductId:X4}:{pad.NonRoamableId}",
                Name: name,
                Kind: DeviceKind.Gamepad,
                Percent: percent,
                IsConnected: true,
                IsCharging: charging));
        }

        if (results.Count == 0)
        {
            AddXinputFallback(results, vendor);
        }
        else if (vendor?.Percent is int vendorPercent)
        {
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Percent is null)
                {
                    results[i] = results[i] with
                    {
                        Percent = vendorPercent,
                        IsCharging = results[i].IsCharging || vendor!.IsCharging
                    };
                }
            }
        }

        if (results.Count == 0 && vendor is not null)
        {
            results.Add(vendor);
        }

        return Task.FromResult<IReadOnlyList<AccessoryBattery>>(results);
    }

    private static void AddXinputFallback(List<AccessoryBattery> results, AccessoryBattery? vendor)
    {
        for (uint slot = 0; slot < 4; slot++)
        {
            if (XInputGetState(slot, out _) != 0)
            {
                continue;
            }

            int? percent = vendor?.Percent;
            var charging = vendor?.IsCharging ?? false;
            if (percent is null
                && XInputGetBatteryInformation(slot, 0, out var info) == 0)
            {
                percent = PercentFromXinput(info);
            }

            results.Add(new AccessoryBattery(
                Id: $"xinput:{slot}",
                Name: "Manba One V2",
                Kind: DeviceKind.Gamepad,
                Percent: percent,
                IsConnected: true,
                IsCharging: charging));
        }
    }

    private static string FriendlyName(string displayName, ushort vendorId, ushort productId)
    {
        if (vendorId == Xbox360VendorId && productId == Xbox360ProductId)
        {
            return "Manba One V2";
        }

        var haystack = $"{displayName}".ToLowerInvariant();
        if (haystack.Contains("manba"))
        {
            return displayName.Contains("V2", StringComparison.OrdinalIgnoreCase)
                ? DeviceKindClassifier.CleanName(displayName)
                : "Manba One V2";
        }

        var cleaned = DeviceKindClassifier.CleanName(displayName);
        return string.IsNullOrWhiteSpace(cleaned) ? "Manba One V2" : cleaned;
    }

    private static int? PercentFromReport(BatteryReport? report)
    {
        if (report is null || IsDummyReport(report))
        {
            return null;
        }

        var remain = report.RemainingCapacityInMilliwattHours;
        var full = report.FullChargeCapacityInMilliwattHours;
        if (remain is int remaining && full is int capacity && capacity > 0)
        {
            return Math.Clamp((int)Math.Round(100.0 * remaining / capacity), 0, 100);
        }

        return null;
    }

    private static bool IsReallyCharging(BatteryReport? report) =>
        report is not null && !IsDummyReport(report) && report.Status == BatteryStatus.Charging;

    private static bool IsDummyReport(BatteryReport report)
    {
        var remain = report.RemainingCapacityInMilliwattHours;
        var full = report.FullChargeCapacityInMilliwattHours;
        var design = report.DesignCapacityInMilliwattHours;
        if (remain is not int remaining || full is not int capacity || capacity <= 0)
        {
            return true;
        }

        if (remaining != capacity)
        {
            return false;
        }

        return design is null && capacity is 1 or 100 or 1000;
    }

    private static int? PercentFromXinput(XInputBatteryInformation info)
    {
        // Wired clones (Manba on the 2.4 GHz dongle) always report FULL.
        if (info.BatteryType is 0 or 1 or 255)
        {
            return null;
        }

        return info.BatteryLevel switch
        {
            0 => 5,
            1 => 33,
            2 => 66,
            3 => 100,
            _ => null
        };
    }

    [DllImport("xinput1_4.dll")]
    private static extern uint XInputGetState(uint dwUserIndex, out XInputState state);

    [DllImport("xinput1_4.dll")]
    private static extern uint XInputGetBatteryInformation(uint dwUserIndex, byte devType, out XInputBatteryInformation info);

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputBatteryInformation
    {
        public byte BatteryType;
        public byte BatteryLevel;
    }
}
