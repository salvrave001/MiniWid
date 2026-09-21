using System.Text;
using HidSharp;

Console.OutputEncoding = Encoding.UTF8;

foreach (var hid in DeviceList.Local.GetHidDevices())
{
    if (hid.VendorID is not 0x045E and not 0x1A34 and not 0x04B4 and not 0x37D7)
    {
        continue;
    }

    try
    {
        Console.WriteLine($"{hid.VendorID:X4}:{hid.ProductID:X4} in={hid.GetMaxInputReportLength()} out={hid.GetMaxOutputReportLength()} feat={hid.GetMaxFeatureReportLength()} {hid.GetProductName()}");
        Console.WriteLine($"  {hid.DevicePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{hid.VendorID:X4}:{hid.ProductID:X4} {ex.Message}");
    }
}
