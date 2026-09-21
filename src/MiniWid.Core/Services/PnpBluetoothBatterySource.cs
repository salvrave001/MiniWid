using System.Text;
using System.Text.RegularExpressions;
using MiniWid.Core.Models;
using MiniWid.Core.Native;

namespace MiniWid.Core.Services;

public sealed class PnpBluetoothBatterySource : IBatterySource
{
    private static readonly Regex AddressFromId = new(
        @"([0-9A-F]{2}[:\-]?){5}[0-9A-F]{2}|[0-9A-F]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<AccessoryBattery>();
        var connected = await BluetoothConnection.GetConnectedAddressesAsync(cancellationToken).ConfigureAwait(false);

        if (NativeMethods.CM_Get_Device_ID_List_SizeW(
                out var length,
                null,
                NativeMethods.CmGetIdListFilterPresent) != NativeMethods.CrSuccess
            || length == 0)
        {
            return results;
        }

        var buffer = new char[length];
        if (NativeMethods.CM_Get_Device_ID_ListW(
                null,
                buffer,
                length,
                NativeMethods.CmGetIdListFilterPresent) != NativeMethods.CrSuccess)
        {
            return results;
        }

        foreach (var deviceId in SplitMultiSz(buffer))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (NativeMethods.CM_Locate_DevNodeW(
                    out var devInst,
                    deviceId,
                    NativeMethods.CmLocateDevnodeNormal) != NativeMethods.CrSuccess)
            {
                continue;
            }

            if (!TryReadBatteryPercent(devInst, out var percent))
            {
                continue;
            }

            var name = TryReadString(devInst, NativeMethods.DeviceFriendlyName)
                       ?? TryReadString(devInst, NativeMethods.Name)
                       ?? deviceId;

            if (DeviceKindClassifier.IsNoiseName(name))
            {
                continue;
            }

            name = DeviceKindClassifier.CleanName(name);
            var deviceClass = TryReadString(devInst, NativeMethods.DeviceClass);
            var address = NormalizeAddress(
                TryReadString(devInst, NativeMethods.BluetoothDeviceAddress)
                ?? ExtractAddress(deviceId));

            var isBluetoothNode = deviceId.Contains("BTH", StringComparison.OrdinalIgnoreCase);
            if (isBluetoothNode)
            {
                if (string.IsNullOrWhiteSpace(address) || !connected.Contains(address))
                {
                    continue;
                }
            }

            results.Add(new AccessoryBattery(
                Id: deviceId,
                Name: name,
                Kind: DeviceKindClassifier.Classify(name, deviceClass),
                Percent: percent,
                IsConnected: !isBluetoothNode || connected.Contains(address!),
                IsCharging: false,
                Address: address));
        }

        return results;
    }

    private static bool TryReadBatteryPercent(uint devInst, out int percent)
    {
        percent = 0;
        if (!TryReadProperty(
                devInst,
                NativeMethods.BluetoothBatteryLevel,
                out var type,
                out var buffer)
            || buffer.Length == 0)
        {
            return false;
        }

        var rawType = type & NativeMethods.DevPropTypeMask;
        int value = rawType switch
        {
            NativeMethods.DevPropTypeByte => buffer[0],
            NativeMethods.DevPropTypeUint16 when buffer.Length >= 2 => BitConverter.ToUInt16(buffer, 0),
            NativeMethods.DevPropTypeUint32 when buffer.Length >= 4 => (int)BitConverter.ToUInt32(buffer, 0),
            _ => buffer[0]
        };

        if (value is < 0 or > 100)
        {
            return false;
        }

        percent = value;
        return true;
    }

    private static string? TryReadString(uint devInst, NativeMethods.DevPropKey key)
    {
        if (!TryReadProperty(devInst, key, out _, out var buffer) || buffer.Length < 2)
        {
            return null;
        }

        var text = Encoding.Unicode.GetString(buffer).TrimEnd('\0').Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool TryReadProperty(
        uint devInst,
        NativeMethods.DevPropKey key,
        out uint type,
        out byte[] buffer)
    {
        type = 0;
        uint size = 0;
        NativeMethods.CM_Get_DevNode_PropertyW(devInst, in key, out type, null, ref size, 0);
        if (size == 0)
        {
            buffer = [];
            return false;
        }

        buffer = new byte[size];
        var status = NativeMethods.CM_Get_DevNode_PropertyW(devInst, in key, out type, buffer, ref size, 0);
        return status == NativeMethods.CrSuccess;
    }

    private static IEnumerable<string> SplitMultiSz(char[] buffer)
    {
        var start = 0;
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != '\0')
            {
                continue;
            }

            var length = i - start;
            if (length <= 0)
            {
                yield break;
            }

            yield return new string(buffer, start, length);
            start = i + 1;
        }
    }

    private static string? ExtractAddress(string deviceId)
    {
        var match = AddressFromId.Match(deviceId);
        return match.Success ? match.Value : null;
    }

    private static string? NormalizeAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var hex = Regex.Replace(value, "[^0-9A-Fa-f]", string.Empty).ToUpperInvariant();
        return hex.Length == 12 ? hex : value.Trim().ToUpperInvariant();
    }
}
