using System.Collections.Frozen;
using Core.Configs;
using Core.Enums;
using Core.Models.RateLimitOverrides;

namespace Presentation.RateLimiting;

public sealed class RateLimitOverrideSnapshot
{
    private FrozenDictionary<(Guid OrganizationId, RateLimitPool Pool), RateLimitOverride> _overrides =
        FrozenDictionary<(Guid, RateLimitPool), RateLimitOverride>.Empty;

    public void Replace(IReadOnlyCollection<RateLimitOverride> rateLimitOverrides)
    {
        var resolved = new Dictionary<(Guid OrganizationId, RateLimitPool Pool), RateLimitOverride>();

        // All-pools entries first so a pool-specific override replaces them.
        foreach (var rateLimitOverride in rateLimitOverrides.Where(x => !x.Pool.HasValue))
        {
            foreach (var pool in Enum.GetValues<RateLimitPool>())
            {
                resolved[(rateLimitOverride.OrganizationId, pool)] = rateLimitOverride;
            }
        }

        foreach (var rateLimitOverride in rateLimitOverrides.Where(x => x.Pool.HasValue))
        {
            resolved[(rateLimitOverride.OrganizationId, rateLimitOverride.Pool!.Value)] = rateLimitOverride;
        }

        _overrides = resolved.ToFrozenDictionary();
    }

    public RateLimitOverride? Find(Guid organizationId, RateLimitPool pool)
    {
        return _overrides.TryGetValue((organizationId, pool), out var rateLimitOverride) ? rateLimitOverride : null;
    }

    public static RateLimitBucketConfig ToBucketConfig(RateLimitOverride rateLimitOverride, RateLimitBucketConfig configured)
    {
        return configured with
        {
            PermitLimit = rateLimitOverride.PermitLimit,
            TokensPerPeriod = rateLimitOverride.TokensPerPeriod,
            ReplenishmentPeriodSeconds = rateLimitOverride.ReplenishmentPeriod
        };
    }
}
