using Core.Enums;
using Core.Interfaces;
using Core.ValueObjects;

namespace Core.Helpers;

// Jobs have no caller to establish a mode, so each sweep runs once per mode rather than reading across both.
public static class ModeSweepHelper
{
    public static async Task<Count> SweepEachModeAsync(IModeContext modeContext, Func<Task<Count>> sweepAsync)
    {
        var total = 0;

        // Pin, never assign: assigning null latches "no mode" and leaks across the partition in a request scope.
        foreach (var mode in Enum.GetValues<Mode>())
        {
            using var pinned = modeContext.Pin(mode);

            total += await sweepAsync();
        }

        return new Count(total);
    }
}
