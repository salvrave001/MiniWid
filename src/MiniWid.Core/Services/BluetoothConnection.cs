using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace MiniWid.Core.Services;

internal static class BluetoothConnection
{
    public static async Task<HashSet<string>> GetConnectedAddressesAsync(CancellationToken cancellationToken)
    {
        var connected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await AddAsync(
            BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected),
            classic: true,
            connected,
            cancellationToken).ConfigureAwait(false);
        await AddAsync(
            BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected),
            classic: false,
            connected,
            cancellationToken).ConfigureAwait(false);
        return connected;
    }

    private static async Task AddAsync(
        string selector,
        bool classic,
        HashSet<string> connected,
        CancellationToken cancellationToken)
    {
        try
        {
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken).ConfigureAwait(false);
            foreach (var info in devices)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (classic)
                    {
                        using var device = await BluetoothDevice.FromIdAsync(info.Id).AsTask(cancellationToken).ConfigureAwait(false);
                        if (device is not null)
                        {
                            connected.Add(device.BluetoothAddress.ToString("X12"));
                        }
                    }
                    else
                    {
                        using var device = await BluetoothLEDevice.FromIdAsync(info.Id).AsTask(cancellationToken).ConfigureAwait(false);
                        if (device is not null)
                        {
                            connected.Add(device.BluetoothAddress.ToString("X12"));
                        }
                    }
                }
                catch
                {
                    // Device can disappear between enumeration and open.
                }
            }
        }
        catch
        {
            // Bluetooth radio may be off.
        }
    }
}
