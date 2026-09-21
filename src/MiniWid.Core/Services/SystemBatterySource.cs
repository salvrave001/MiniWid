using MiniWid.Core.Models;
using MiniWid.Core.Native;

namespace MiniWid.Core.Services;

public sealed class SystemBatterySource : IBatterySource
{
    public Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            return Task.FromResult<IReadOnlyList<AccessoryBattery>>([]);
        }

        if ((status.BatteryFlag & NativeMethods.BatteryFlagNoSystemBattery) != 0
            || status.BatteryLifePercent == NativeMethods.BatteryPercentUnknown)
        {
            return Task.FromResult<IReadOnlyList<AccessoryBattery>>([]);
        }

        var percent = Math.Clamp((int)status.BatteryLifePercent, 0, 100);
        var charging = status.ACLineStatus == 1
            || (status.BatteryFlag & NativeMethods.BatteryFlagCharging) != 0;

        AccessoryBattery device = new(
            Id: "system-battery",
            Name: "This PC",
            Kind: DeviceKind.Laptop,
            Percent: percent,
            IsConnected: true,
            IsCharging: charging,
            IsSystem: true);

        return Task.FromResult<IReadOnlyList<AccessoryBattery>>([device]);
    }
}
