using System.Runtime.InteropServices;

namespace MiniWid.Core.Native;

internal static class NativeMethods
{
    public const int CrSuccess = 0;
    public const uint CmLocateDevnodeNormal = 0;
    public const uint CmGetIdListFilterPresent = 0x00000100;
    public const uint CmGetIdListFilterNone = 0;

    public const uint DevPropTypeByte = 0x00000003;
    public const uint DevPropTypeUint16 = 0x00000005;
    public const uint DevPropTypeUint32 = 0x00000007;
    public const uint DevPropTypeString = 0x00000012;
    public const uint DevPropTypeMask = 0x00000FFF;

    public const byte BatteryFlagNoSystemBattery = 128;
    public const byte BatteryPercentUnknown = 255;
    public const byte BatteryFlagCharging = 8;

    [StructLayout(LayoutKind.Sequential)]
    public struct DevPropKey
    {
        public Guid Fmtid;
        public uint Pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    public static readonly DevPropKey BluetoothBatteryLevel = new()
    {
        Fmtid = new Guid(0x104EA319, 0x6EE2, 0x4701, 0xBD, 0x47, 0x8D, 0xDB, 0xF4, 0x25, 0xBB, 0xE5),
        Pid = 2
    };

    public static readonly DevPropKey DeviceFriendlyName = new()
    {
        Fmtid = new Guid(0xA45C254E, 0xDF1C, 0x4EFD, 0x80, 0x20, 0x67, 0xD1, 0x46, 0xA8, 0x50, 0xE0),
        Pid = 14
    };

    public static readonly DevPropKey Name = new()
    {
        Fmtid = new Guid(0xB725F130, 0x47EF, 0x101A, 0xA5, 0xF1, 0x02, 0x60, 0x8C, 0x9E, 0xEB, 0xAC),
        Pid = 10
    };

    public static readonly DevPropKey BluetoothDeviceAddress = new()
    {
        Fmtid = new Guid(0x2BD67D8B, 0x8BEB, 0x48D5, 0x87, 0xE0, 0x6C, 0xDA, 0x34, 0x28, 0x04, 0x0A),
        Pid = 1
    };

    public static readonly DevPropKey DeviceClass = new()
    {
        Fmtid = new Guid(0xA45C254E, 0xDF1C, 0x4EFD, 0x80, 0x20, 0x67, 0xD1, 0x46, 0xA8, 0x50, 0xE0),
        Pid = 9
    };

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_ID_List_SizeW(out uint pulLen, string? pszFilter, uint ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_ID_ListW(string? pszFilter, char[] buffer, uint bufferLen, uint ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_DevNode_PropertyW(
        uint dnDevInst,
        in DevPropKey PropertyKey,
        out uint PropertyType,
        byte[]? PropertyBuffer,
        ref uint PropertyBufferSize,
        uint ulFlags);
}
