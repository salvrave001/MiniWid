using HidSharp;
using MiniWid.Core.Models;

namespace MiniWid.Core.Services;

internal static class ManbaVendorHid
{
    private const int VendorId = 0x1A34;
    private const int ProductId = 0xF517;

    public static AccessoryBattery? TryRead(CancellationToken cancellationToken)
    {
        foreach (var hid in DeviceList.Local.GetHidDevices(VendorId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hid.ProductID != ProductId)
            {
                continue;
            }

            if (TryRead(hid, out var percent, out var charging))
            {
                return new AccessoryBattery(
                    Id: hid.DevicePath,
                    Name: "Manba One V2",
                    Kind: DeviceKind.Gamepad,
                    Percent: percent,
                    IsConnected: true,
                    IsCharging: charging);
            }
        }

        return null;
    }

    private static bool TryRead(HidDevice hid, out int? percent, out bool charging)
    {
        percent = null;
        charging = false;

        var inLen = hid.GetMaxInputReportLength();
        var outLen = hid.GetMaxOutputReportLength();
        if (inLen <= 0 || outLen <= 0)
        {
            return false;
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
            stream.ReadTimeout = 80;
            stream.WriteTimeout = 200;
            Drain(stream, inLen);

            foreach (var packet in BuildQueries(outLen))
            {
                try
                {
                    stream.Write(packet);
                }
                catch
                {
                    continue;
                }

                var until = DateTime.UtcNow.AddMilliseconds(350);
                while (DateTime.UtcNow < until)
                {
                    try
                    {
                        var buf = new byte[inLen];
                        var n = stream.Read(buf);
                        if (n <= 0 || !TryParse(buf.AsSpan(0, n), out percent, out charging))
                        {
                            continue;
                        }

                        return percent is not null;
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }
            }
        }

        return false;
    }

    private static IEnumerable<byte[]> BuildQueries(int outLen)
    {
        yield return Prefix(outLen, [0x5A, 0xA5, 0x01, 0x02, 0x02]);
        yield return Prefix(outLen, [0x03, 0x5A, 0xA5, 0x01, 0x02, 0x05]);
        yield return Flydigi(outLen, cmd: 0x01, innerReport: true);
        yield return Flydigi(outLen, cmd: 0x01, innerReport: false);
        yield return Prefix(outLen, [0xA5, 0x10]);
        yield return Prefix(outLen, [0xA5, 0x01]);
    }

    private static byte[] Flydigi(int length, byte cmd, bool innerReport)
    {
        var buf = new byte[length];
        var i = 1;
        if (innerReport)
        {
            buf[i++] = 0x03;
        }

        var magic = i;
        buf[i++] = 0x5A;
        buf[i++] = 0xA5;
        buf[i++] = cmd;
        buf[i++] = 0x02;
        var sum = 0;
        for (var j = magic; j < i; j++)
        {
            sum += buf[j];
        }

        buf[i] = (byte)(sum & 0xFF);
        return buf;
    }

    private static byte[] Prefix(int length, byte[] head)
    {
        var buf = new byte[length];
        Array.Copy(head, 0, buf, 1, Math.Min(head.Length, length - 1));
        return buf;
    }

    private static void Drain(HidStream stream, int inLen)
    {
        var previous = stream.ReadTimeout;
        stream.ReadTimeout = 15;
        try
        {
            for (var i = 0; i < 6; i++)
            {
                stream.Read(new byte[inLen]);
            }
        }
        catch
        {
            // Nothing pending.
        }

        stream.ReadTimeout = previous;
    }

    internal static bool TryParse(ReadOnlySpan<byte> data, out int? percent, out bool charging)
    {
        percent = null;
        charging = false;
        if (data.Length < 8)
        {
            return false;
        }

        var magic = IndexOf(data, [0x5A, 0xA5]);
        if (magic >= 0 && magic + 12 < data.Length && data[magic + 2] is 0x01)
        {
            return DecodeNibble(data[magic + 11], out percent, out charging);
        }

        if (data.Length > 23 && data[14] == 0xA5 && data[15] is 0x01 or 0x10)
        {
            return DecodeNibble(data[23], out percent, out charging);
        }

        return false;
    }

    private static bool DecodeNibble(byte raw, out int? percent, out bool charging)
    {
        var status = (raw >> 4) & 0x0F;
        var level = raw & 0x0F;
        charging = status == 1 || level == 6;
        if (status == 2)
        {
            percent = 100;
            charging = false;
            return true;
        }

        if (level is >= 1 and <= 5)
        {
            percent = level * 20;
            return true;
        }

        if (level == 0 && status is 0 or 1)
        {
            percent = charging ? 5 : 0;
            return true;
        }

        if (raw is > 0 and <= 100)
        {
            percent = raw;
            charging = false;
            return true;
        }

        percent = null;
        charging = false;
        return false;
    }

    private static int IndexOf(ReadOnlySpan<byte> data, ReadOnlySpan<byte> needle)
    {
        for (var i = 0; i <= data.Length - needle.Length; i++)
        {
            if (data[i..].StartsWith(needle))
            {
                return i;
            }
        }

        return -1;
    }
}
