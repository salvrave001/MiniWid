namespace MiniWid.Core.Models;

public sealed record AccessoryBattery(
    string Id,
    string Name,
    DeviceKind Kind,
    int? Percent,
    bool IsConnected,
    bool IsCharging,
    bool IsAsleep = false,
    bool IsSystem = false,
    string? Address = null);
