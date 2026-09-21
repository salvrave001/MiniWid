using MiniWid.Core.Models;

namespace MiniWid.Core.Services;

public sealed class BatteryAggregator
{
    private readonly IReadOnlyList<IBatterySource> _sources;

    public BatteryAggregator(IEnumerable<IBatterySource>? sources = null)
    {
        _sources = sources?.ToArray() ??
        [
            new SystemBatterySource(),
            new RogDelta2HidSource(),
            new WlMouseHidSource(),
            new GamepadBatterySource(),
            new PnpBluetoothBatterySource(),
            new BleGattBatterySource()
        ];
    }

    public async Task<IReadOnlyList<AccessoryBattery>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var merged = new Dictionary<string, AccessoryBattery>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in _sources)
        {
            IReadOnlyList<AccessoryBattery> devices;
            try
            {
                devices = await source.GetAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                continue;
            }

            foreach (var device in devices)
            {
                var key = BuildKey(device);
                if (merged.TryGetValue(key, out var existing))
                {
                    merged[key] = Merge(existing, device);
                }
                else
                {
                    merged[key] = device;
                }
            }
        }

        return merged.Values
            .Where(d => d.IsConnected)
            .OrderByDescending(d => d.IsSystem)
            .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static string BuildKey(AccessoryBattery device)
    {
        if (device.IsSystem)
        {
            return "system";
        }

        if (!string.IsNullOrWhiteSpace(device.Address))
        {
            return $"addr:{device.Address}";
        }

        return $"name:{device.Name.Trim()}";
    }

    private static AccessoryBattery Merge(AccessoryBattery left, AccessoryBattery right)
    {
        var preferredName = PreferName(left.Name, right.Name);
        var kind = left.Kind != DeviceKind.Other ? left.Kind : right.Kind;
        if (kind == DeviceKind.Other)
        {
            kind = DeviceKindClassifier.Classify(preferredName);
        }

        return left with
        {
            Name = preferredName,
            Kind = kind,
            Percent = right.Percent ?? left.Percent,
            IsConnected = left.IsConnected || right.IsConnected,
            IsCharging = left.IsCharging || right.IsCharging,
            IsAsleep = left.IsAsleep || right.IsAsleep,
            Address = left.Address ?? right.Address
        };
    }

    private static string PreferName(string left, string right)
    {
        var leftNoise = DeviceKindClassifier.IsNoiseName(left) || left.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase);
        var rightNoise = DeviceKindClassifier.IsNoiseName(right) || right.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase);
        if (leftNoise && !rightNoise)
        {
            return right;
        }

        if (!leftNoise && rightNoise)
        {
            return left;
        }

        return left.Length >= right.Length ? left : right;
    }
}
