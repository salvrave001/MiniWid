using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MiniWid.App.Services;
using MiniWid.Core.Models;
using MiniWid.Core.Services;
using Windows.UI;

namespace MiniWid.App.ViewModels;

public sealed class DeviceRowViewModel
{
    public DeviceRowViewModel(
        AccessoryBattery device,
        string connected,
        string disconnected,
        string charging,
        string sleeping,
        string thisPc,
        string fillHealthy,
        string fillWarn,
        string fillLow)
    {
        Name = device.IsSystem ? thisPc : device.Name;
        Kind = device.Kind;
        Glyph = device.Kind switch
        {
            DeviceKind.Laptop => "\uE7F8",
            DeviceKind.Headphones => "\uE7F6",
            DeviceKind.Keyboard => "\uE92E",
            DeviceKind.Mouse => "\uE962",
            DeviceKind.Gamepad => "\uE7FC",
            _ => "\uE702"
        };
        Percent = device.Percent;
        PercentText = device.IsAsleep
            ? sleeping
            : device.Percent is int value ? $"{value}%" : "—";
        PercentFontSize = device.IsAsleep ? 13.0 : 22.0;
        PercentWrapping = device.IsAsleep ? TextWrapping.Wrap : TextWrapping.NoWrap;
        Status = device.IsCharging
            ? charging
            : device.IsConnected ? connected : disconnected;
        FillRatio = Math.Clamp((device.Percent ?? 0) / 100.0, 0, 1);
        FillWidth = FillRatio * 18.0;
        PercentValue = device.Percent ?? 0;
        IsCharging = device.IsCharging && !device.IsAsleep;
        ChargingVisibility = IsCharging ? Visibility.Visible : Visibility.Collapsed;
        BatteryIconVisibility = IsCharging || device.IsAsleep ? Visibility.Collapsed : Visibility.Visible;
        NameWrapping = IsCharging ? TextWrapping.Wrap : TextWrapping.NoWrap;
        NameMaxLines = IsCharging ? 2 : 1;
        FillHex = (device.Percent ?? 100) switch
        {
            < 15 => fillLow,
            < 30 => fillWarn,
            _ => fillHealthy
        };
        FillBrush = new SolidColorBrush(ToColor(FillHex));
        Photo = DeviceArt.Resolve(Name, Kind);
        PhotoVisibility = Photo is null ? Visibility.Collapsed : Visibility.Visible;
        GlyphVisibility = Photo is null ? Visibility.Visible : Visibility.Collapsed;
        (PhotoScale, PhotoOffsetX) = DeviceArt.Layout(Name, Kind);
    }

    public string Name { get; }
    public DeviceKind Kind { get; }
    public string Glyph { get; }
    public int? Percent { get; }
    public string PercentText { get; }
    public double PercentFontSize { get; }
    public TextWrapping PercentWrapping { get; }
    public string Status { get; }
    public double FillRatio { get; }
    public double FillWidth { get; }
    public int PercentValue { get; }
    public bool IsCharging { get; }
    public Visibility ChargingVisibility { get; }
    public Visibility BatteryIconVisibility { get; }
    public TextWrapping NameWrapping { get; }
    public int NameMaxLines { get; }
    public string FillHex { get; }
    public SolidColorBrush FillBrush { get; }
    public BitmapImage? Photo { get; }
    public Visibility PhotoVisibility { get; }
    public Visibility GlyphVisibility { get; }
    public double PhotoScale { get; }
    public double PhotoOffsetX { get; }

    private static Color ToColor(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(
            255,
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }
}

public sealed class WidgetViewModel : INotifyPropertyChanged
{
    private readonly BatteryAggregator _aggregator = new();
    private string _statusMessage = string.Empty;
    private bool _isEmpty;

    public ObservableCollection<DeviceRowViewModel> Devices { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetField(ref _isEmpty, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task RefreshAsync(
        bool showThisPc,
        string connected,
        string disconnected,
        string charging,
        string sleeping,
        string thisPc,
        string emptyMessage,
        string fillHealthy,
        string fillWarn,
        string fillLow,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AccessoryBattery> devices;
        try
        {
            devices = await _aggregator.GetDevicesAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            devices = [];
        }

        var rows = devices
            .Where(d => d.IsConnected)
            .Where(d => showThisPc || !d.IsSystem)
            .Select(d => new DeviceRowViewModel(d, connected, disconnected, charging, sleeping, thisPc, fillHealthy, fillWarn, fillLow))
            .ToList();

        Devices.Clear();
        foreach (var row in rows)
        {
            Devices.Add(row);
        }

        IsEmpty = Devices.Count == 0;
        StatusMessage = IsEmpty ? emptyMessage : string.Empty;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
