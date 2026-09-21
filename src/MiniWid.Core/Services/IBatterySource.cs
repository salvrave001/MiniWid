using MiniWid.Core.Models;

namespace MiniWid.Core.Services;

public interface IBatterySource
{
    Task<IReadOnlyList<AccessoryBattery>> GetAsync(CancellationToken cancellationToken = default);
}
