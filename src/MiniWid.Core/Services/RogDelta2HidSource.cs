using HidSharp;
using MiniWid.Core.Models;

namespace MiniWid.Core.Services;

public sealed class RogDelta2HidSource : IBatterySource
{
    private const int AsusVendorId = 0x0B05;
    private static readonly int[] ProductIds = [0x1D41, 0x1AFA];

    public Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<AccessoryBattery>();

        foreach (var hid in DeviceList.Local.GetHidDevices(AsusVendorId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ProductIds.Contains(hid.ProductID))
            {
                continue;
            }

            if (hid.GetMaxInputReportLength() != 64 || hid.GetMaxOutputReportLength() != 64)
            {
                continue;
            }

            if (!TryRead(hid, out var percent, out var charging, out var alive, out var name))
            {
                continue;
            }

            if (!alive || percent is null)
            {
                continue;
            }

            results.Add(new AccessoryBattery(
                Id: hid.DevicePath,
                Name: name,
                Kind: DeviceKind.Headphones,
                Percent: percent,
                IsConnected: true,
                IsCharging: charging));
            break;
        }

        return Task.FromResult<IReadOnlyList<AccessoryBattery>>(results);
    }

    private static bool TryRead(
        HidDevice hid,
        out int? percent,
        out bool charging,
        out bool alive,
        out string name)
    {
        percent = null;
        charging = false;
        alive = false;
        name = "ROG DELTA II";

        try
        {
            name = DeviceKindClassifier.CleanName(hid.GetProductName());
        }
        catch
        {
            // Keep default name.
        }

        var config = new OpenConfiguration();
        config.SetOption(OpenOption.Exclusive, false);
        config.SetOption(OpenOption.Interruptible, true);
        config.SetOption(OpenOption.Priority, OpenPriority.Low);
        if (!hid.TryOpen(config, out var stream))
        {
            return false;
        }

        using (stream)
        {
            stream.ReadTimeout = 800;
            stream.WriteTimeout = 800;

            if (TryQuery(stream, key: 0x00, index1: 0x01, out var connection) && connection.Length > 5)
            {
                alive = connection[5] == 1;
                if (!alive)
                {
                    return true;
                }
            }

            if (TryQuery(stream, key: 0x09, index1: 0x00, out var batteryNotify) && batteryNotify.Length > 5)
            {
                percent = AsPercent(batteryNotify[5]);
            }

            if (percent is null && TryQuery(stream, key: 0x07, index1: 0x00, out var batteryGet) && batteryGet.Length > 6)
            {
                percent = AsPercent(batteryGet[6]) ?? AsPercent(batteryGet[5]);
            }

            if (TryQuery(stream, key: 0x08, index1: 0x00, out var charge) && charge.Length > 5)
            {
                charging = charge[5] == 1;
            }

            if (percent is not null)
            {
                alive = true;
            }
        }

        return percent is not null || alive;
    }

    private static bool TryQuery(HidStream stream, byte key, byte index1, out byte[] reply)
    {
        reply = [];
        var packet = new byte[64];
        packet[0] = 0xCC;
        packet[1] = 0x12;
        packet[2] = key;
        packet[3] = 0x00;
        packet[4] = index1;

        try
        {
            stream.Write(packet);
            reply = new byte[64];
            stream.Read(reply);
            return reply[0] == 0xCC && reply[1] == 0x12;
        }
        catch
        {
            reply = [];
            return false;
        }
    }

    private static int? AsPercent(byte value) => value <= 100 ? value : null;
}
