using HidSharp;
using MiniWid.Core.Models;

namespace MiniWid.Core.Services;

public sealed class WlMouseHidSource : IBatterySource
{
    private const int VendorId = 0x36A7;
    private const int FeatureLength = 65;
    private const byte BatteryCommand = 0x83;

    private static int? _lastAwakePercent;

    public Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(cancellationToken), cancellationToken);

    private static IReadOnlyList<AccessoryBattery> Read(CancellationToken cancellationToken)
    {
        var results = new List<AccessoryBattery>();

        foreach (var hid in DeviceList.Local.GetHidDevices(VendorId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hid.GetMaxFeatureReportLength() != FeatureLength)
            {
                continue;
            }

            if (!TryRead(hid, out var percent, out var charging, out var asleep, out var name))
            {
                continue;
            }

            if (!asleep && percent is null)
            {
                continue;
            }

            results.Add(new AccessoryBattery(
                Id: hid.DevicePath,
                Name: name,
                Kind: DeviceKind.Mouse,
                Percent: percent,
                IsConnected: true,
                IsCharging: charging && !asleep,
                IsAsleep: asleep));
            break;
        }

        return results;
    }

    private static bool TryRead(HidDevice hid, out int? percent, out bool charging, out bool asleep, out string name)
    {
        percent = null;
        charging = false;
        asleep = false;
        name = FriendlyName(hid.ProductID, hid);

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
            try
            {
                var request = new byte[FeatureLength];
                request[3] = 0x02;
                request[4] = 0x02;
                request[6] = BatteryCommand;
                stream.SetFeature(request);
                Thread.Sleep(100);

                var reply = new byte[FeatureLength];
                stream.GetFeature(reply);
                if (reply[3] != 0x02 || reply[4] != 0x02 || reply[6] != BatteryCommand)
                {
                    return false;
                }

                charging = reply[7] != 0;
                var raw = reply[8];
                asleep = !charging && raw == 0;
                if (asleep)
                {
                    percent = _lastAwakePercent;
                }
                else
                {
                    percent = AsPercent(raw);
                    if (percent is > 0)
                    {
                        _lastAwakePercent = percent;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        return percent is not null || asleep;
    }

    private static string FriendlyName(int productId, HidDevice hid)
    {
        return productId switch
        {
            0xA882 => "WLMouse Beast X Max 8K",
            0xA883 or 0xA884 => "WLMouse Beast X 8K",
            0xA887 or 0xA888 => "WLMouse Beast X",
            _ => ProductOrFallback(hid)
        };
    }

    private static string ProductOrFallback(HidDevice hid)
    {
        try
        {
            var product = DeviceKindClassifier.CleanName(hid.GetProductName());
            if (!string.IsNullOrWhiteSpace(product)
                && !product.Contains("RECEIVER", StringComparison.OrdinalIgnoreCase))
            {
                return product;
            }
        }
        catch
        {
            // Keep fallback name.
        }

        return "WLMouse Beast X Max 8K";
    }

    private static int? AsPercent(byte value) => value <= 100 ? value : null;
}
