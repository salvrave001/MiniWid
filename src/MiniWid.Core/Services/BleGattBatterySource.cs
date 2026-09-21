using MiniWid.Core.Models;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace MiniWid.Core.Services;

public sealed class BleGattBatterySource : IBatterySource
{
    public async Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<AccessoryBattery>();

        try
        {
            var selector = GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.Battery);
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);

            foreach (var info in devices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var service = await GattDeviceService.FromIdAsync(info.Id).AsTask(cancellationToken);
                    if (service is null || service.Device.ConnectionStatus != BluetoothConnectionStatus.Connected)
                    {
                        continue;
                    }

                    var characteristics = await service
                        .GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel)
                        .AsTask(cancellationToken);

                    if (characteristics.Status != GattCommunicationStatus.Success
                        || characteristics.Characteristics.Count == 0)
                    {
                        continue;
                    }

                    var characteristic = characteristics.Characteristics[0];
                    var read = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);
                    if (read.Status != GattCommunicationStatus.Success || read.Value is null)
                    {
                        continue;
                    }

                    var reader = DataReader.FromBuffer(read.Value);
                    var percent = reader.ReadByte();
                    if (percent > 100)
                    {
                        continue;
                    }

                    var name = DeviceKindClassifier.CleanName(
                        string.IsNullOrWhiteSpace(info.Name) ? service.Device.Name : info.Name);

                    if (DeviceKindClassifier.IsNoiseName(name))
                    {
                        continue;
                    }

                    results.Add(new AccessoryBattery(
                        Id: info.Id,
                        Name: name,
                        Kind: DeviceKindClassifier.Classify(name),
                        Percent: percent,
                        IsConnected: true,
                        IsCharging: false,
                        Address: service.Device.BluetoothAddress.ToString("X12")));
                }
                catch (UnauthorizedAccessException)
                {
                    // GATT access requires user consent on some builds.
                }
                catch (ArgumentException)
                {
                    // Device disappeared between enumeration and open.
                }
            }
        }
        catch (Exception)
        {
            // Bluetooth stack can be unavailable (airplane mode, no radio).
        }

        return results;
    }
}
