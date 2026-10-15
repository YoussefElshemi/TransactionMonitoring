using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Presentation.Mappers;

public static class HoldPolicyPatchMapper
{
    public static Patch<HoldWindow?> ToHoldWindow(this Patch<int?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<HoldWindow?>.Set(patch.Value.HasValue ? new HoldWindow(patch.Value.Value) : null);
    }

    public static Patch<SandboxCredits?> ToSandboxCredits(this Patch<int?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<SandboxCredits?>.Set(patch.Value.HasValue ? new SandboxCredits(patch.Value.Value) : null);
    }

    public static Patch<HoldExpiryVerdict?> ToHoldExpiryVerdict(this Patch<string?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<HoldExpiryVerdict?>.Set(patch.Value is null ? null : Enum.Parse<HoldExpiryVerdict>(patch.Value, true));
    }
}
