using System.Runtime.InteropServices;

namespace MiniWid.App.Services;

internal sealed class TrayIconService : IDisposable
{
    public const int WmTrayIcon = 0x0400 + 21;
    public const int WmLButtonUp = 0x0202;
    public const int WmRButtonUp = 0x0205;
    public const int WmLButtonDblClk = 0x0203;
    public const int WmContextMenu = 0x007B;

    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;
    private const uint NimSetVersion = 0x00000004;
    private const uint NotifyIconVersion4 = 4;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NifShowTip = 0x00000080;
    private const uint ImageLoadFromFile = 0x00000010;
    private const uint ImageDefaultSize = 0x00000040;
    private const uint ImageIcon = 1;
    private const int SmCxSmIcon = 49;
    private const int SmCySmIcon = 50;

    private readonly IntPtr _hwnd;
    private readonly uint _id = 1;
    private IntPtr _icon;
    private bool _added;

    public TrayIconService(IntPtr hwnd, string iconPath, string tooltip)
    {
        _hwnd = hwnd;
        _icon = LoadTrayIcon(iconPath);
        AddOrUpdate(tooltip);
    }

    public void UpdateTooltip(string tooltip) => AddOrUpdate(tooltip);

    public void Dispose()
    {
        if (_added)
        {
            var data = CreateData(string.Empty);
            Shell_NotifyIcon(NimDelete, ref data);
            _added = false;
        }

        if (_icon != IntPtr.Zero)
        {
            DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }
    }

    public static (int X, int Y) PointFromCallback(IntPtr wParam)
    {
        var packed = wParam.ToInt64();
        return ((short)packed, (short)(packed >> 16));
    }

    private void AddOrUpdate(string tooltip)
    {
        var data = CreateData(tooltip);
        if (_added)
        {
            Shell_NotifyIcon(NimModify, ref data);
            return;
        }

        if (!Shell_NotifyIcon(NimAdd, ref data))
        {
            return;
        }

        _added = true;
        data.uVersion = NotifyIconVersion4;
        Shell_NotifyIcon(NimSetVersion, ref data);
    }

    private NOTIFYICONDATAW CreateData(string tooltip)
    {
        return new NOTIFYICONDATAW
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = _id,
            uFlags = NifMessage | NifIcon | NifTip | NifShowTip,
            uCallbackMessage = WmTrayIcon,
            hIcon = _icon,
            szTip = tooltip.Length > 127 ? tooltip[..127] : tooltip,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };
    }

    private static IntPtr LoadTrayIcon(string iconPath)
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            ExtractIconEx(exe, 0, out var large, out var small, 1);
            if (small != IntPtr.Zero)
            {
                if (large != IntPtr.Zero && large != small)
                {
                    DestroyIcon(large);
                }

                return small;
            }

            if (large != IntPtr.Zero)
            {
                return large;
            }
        }

        if (!File.Exists(iconPath))
        {
            return IntPtr.Zero;
        }

        var cx = GetSystemMetrics(SmCxSmIcon);
        var cy = GetSystemMetrics(SmCySmIcon);
        return LoadImage(IntPtr.Zero, iconPath, ImageIcon, cx, cy, ImageLoadFromFile | ImageDefaultSize);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public static POINT CursorPosition()
    {
        GetCursorPos(out var point);
        return point;
    }
}
