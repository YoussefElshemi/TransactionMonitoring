using System.Security.Claims;
using System.Threading.RateLimiting;
using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.RateLimitOverrides;
using Core.Models.RateLimits;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Presentation.Attributes;
using Presentation.Extensions;

namespace Presentation.RateLimiting;

public sealed class RequestRateLimiter : IDisposable
{
    private readonly RateLimitConfig _config;
    private readonly IDistributedRateLimiter _distributedRateLimiter;
    private readonly RateLimitOverrideSnapshot _rateLimitOverrideSnapshot;
    private readonly PartitionedRateLimiter<HttpContext> _callerLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _organizationLimiter;

    public RequestRateLimiter(
        IOptions<AppConfig> appConfig,
        IDistributedRateLimiter distributedRateLimiter,
        RateLimitOverrideSnapshot rateLimitOverrideSnapshot)
    {
        _config = appConfig.Value.RateLimitConfig;
        _distributedRateLimiter = distributedRateLimiter;
        _rateLimitOverrideSnapshot = rateLimitOverrideSnapshot;
        _callerLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ResolveCallerPartition);
        _organizationLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ResolveOrganizationPartition);
    }

    public bool IsEnabled => _config.Enabled;

    public static bool IsExempt(HttpContext context)
    {
        return context.GetEndpoint()?.Metadata.GetMetadata<RateLimitExemptAttribute>() != null;
    }

    public async ValueTask<RateLimitResult> AcquireAsync(HttpContext context)
    {
        using var callerLease = await _callerLimiter.AcquireAsync(context, 1, context.RequestAborted);

        if (!callerLease.IsAcquired)
        {
            return Rejected(callerLease, ResolveCallerBucket(context));
        }

        using var organizationLease = await _organizationLimiter.AcquireAsync(context, 1, context.RequestAborted);

        if (!organizationLease.IsAcquired)
        {
            return Rejected(organizationLease, ResolveOrganizationBucket(context));
        }

        if (!_config.Distributed.Enabled)
        {
            return LocallyAllowed(context);
        }

        var decision = await _distributedRateLimiter.AcquireAsync(
            new RateLimitAcquireRequest
            {
                Buckets = ResolveSharedBuckets(context)
            }, context.RequestAborted);

        return decision.Outcome switch
        {
            RateLimitOutcome.Allowed => new RateLimitResult
            {
                IsAllowed = true,
                Limit = (int)decision.PermitLimit!.Value,
                Remaining = (int)decision.RemainingPermits!.Value,
                ResetSeconds = ResolveCallerBucket(context).ReplenishmentPeriodSeconds,
                RetryAfterSeconds = 0
            },
            RateLimitOutcome.Rejected => new RateLimitResult
            {
                IsAllowed = false,
                Limit = (int)decision.PermitLimit!.Value,
                Remaining = (int)decision.RemainingPermits!.Value,
                ResetSeconds = Math.Max(decision.RetryAfter!.Value, 1),
                RetryAfterSeconds = Math.Max(decision.RetryAfter!.Value, 1)
            },
            _ => _config.Distributed.FailOpen
                ? LocallyAllowed(context)
                : Unavailable(ResolveCallerBucket(context))
        };
    }

    public void Dispose()
    {
        _callerLimiter.Dispose();
        _organizationLimiter.Dispose();
    }

    private static RateLimitPool ResolvePool(HttpContext context)
    {
        var declared = context.GetEndpoint()?.Metadata.GetMetadata<RateLimitAttribute>()?.Pool;

        if (declared.HasValue)
        {
            return declared.Value;
        }

        return HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)
            ? RateLimitPool.Read
            : RateLimitPool.Write;
    }

    private static string? ResolveCallerKey(HttpContext context)
    {
        if (context.User.IsApiClient())
        {
            return $"client:{context.User.GetApiClientId()}";
        }

        var applicationUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrWhiteSpace(applicationUserId) ? null : $"user:{applicationUserId}";
    }

    private static string? ResolveOrganizationKey(HttpContext context)
    {
        return context.User.IsApiClient()
            ? RateLimitKeyHelper.OrganizationTag(context.User.GetOrganizationId())
            : null;
    }

    private static Mode ResolveMode(HttpContext context)
    {
        return ModeHeaderHelper.Resolve(context) ?? Mode.Live;
    }

    private static string ResolveAnonymousKey(HttpContext context)
    {
        return $"anon:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private static RateLimitResult Rejected(RateLimitLease lease, RateLimitBucketConfig bucket)
    {
        var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata)
            ? (int)Math.Ceiling(metadata.TotalSeconds)
            : bucket.ReplenishmentPeriodSeconds;

        return new RateLimitResult
        {
            IsAllowed = false,
            Limit = bucket.PermitLimit,
            Remaining = 0,
            ResetSeconds = retryAfter,
            RetryAfterSeconds = Math.Max(retryAfter, 1)
        };
    }

    private static RateLimitResult Unavailable(RateLimitBucketConfig bucket)
    {
        return new RateLimitResult
        {
            IsAllowed = false,
            Limit = bucket.PermitLimit,
            Remaining = 0,
            ResetSeconds = bucket.ReplenishmentPeriodSeconds,
            RetryAfterSeconds = Math.Max(bucket.ReplenishmentPeriodSeconds, 1)
        };
    }

    private static TokenBucketRateLimiterOptions ToOptions(RateLimitBucketConfig bucket)
    {
        return new TokenBucketRateLimiterOptions
        {
            TokenLimit = bucket.PermitLimit,
            TokensPerPeriod = bucket.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(bucket.ReplenishmentPeriodSeconds),
            QueueLimit = bucket.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };
    }

    // Shared hash tag so a Redis Cluster can spend both buckets in one atomic script.
    private IReadOnlyList<RateLimitBucket> ResolveSharedBuckets(HttpContext context)
    {
        var prefix = _config.Distributed.KeyPrefix;
        var pool = ResolvePool(context);
        var callerKey = ResolveCallerKey(context);

        if (callerKey == null)
        {
            return
            [
                ToBucket(RateLimitKeyHelper.ForAnonymous(prefix, ResolveAnonymousKey(context)), _config.AnonymousBucket)
            ];
        }

        var mode = ResolveMode(context);
        var callerBucket = ResolveCallerBucket(context);

        if (!context.User.IsApiClient())
        {
            return
            [
                ToBucket(RateLimitKeyHelper.ForCaller(prefix, callerKey, mode, pool), callerBucket)
            ];
        }

        var organizationId = context.User.GetOrganizationId();

        return
        [
            ToBucket(RateLimitKeyHelper.ForCallerWithinOrganization(prefix, organizationId, callerKey, mode, pool), callerBucket),
            ToBucket(RateLimitKeyHelper.ForOrganization(prefix, organizationId, mode, pool), ResolveOrganizationBucket(context))
        ];
    }

    private static RateLimitBucket ToBucket(string key, RateLimitBucketConfig bucket)
    {
        return new RateLimitBucket
        {
            Key = new RateLimitKey(key),
            PermitLimit = new PermitLimit(bucket.PermitLimit),
            TokensPerPeriod = new TokensPerPeriod(bucket.TokensPerPeriod),
            ReplenishmentPeriod = new ReplenishmentPeriod(bucket.ReplenishmentPeriodSeconds)
        };
    }

    private RateLimitPartition<string> ResolveCallerPartition(HttpContext context)
    {
        var callerKey = ResolveCallerKey(context);

        if (callerKey == null)
        {
            return RateLimitPartition.GetTokenBucketLimiter(
                ResolveAnonymousKey(context), _ => ToOptions(_config.AnonymousBucket));
        }

        var pool = ResolvePool(context);
        var bucket = ResolveCallerBucket(context);

        return RateLimitPartition.GetTokenBucketLimiter(
            $"{callerKey}:{ResolveMode(context)}:{pool}", _ => ToOptions(bucket));
    }

    private RateLimitPartition<string> ResolveOrganizationPartition(HttpContext context)
    {
        var organizationKey = ResolveOrganizationKey(context);

        if (organizationKey == null)
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        var pool = ResolvePool(context);
        var bucket = ResolveOrganizationBucket(context);

        // A partition's options are fixed at build, so the override is part of the key to get a fresh bucket when it changes.
        var rateLimitOverride = FindOverride(context, pool);
        var stamp = rateLimitOverride == null
            ? string.Empty
            : $":override:{rateLimitOverride.RateLimitOverrideId}:{((DateTimeOffset)rateLimitOverride.UpdatedAt).Ticks}";

        return RateLimitPartition.GetTokenBucketLimiter(
            $"{organizationKey}:{ResolveMode(context)}:{pool}{stamp}", _ => ToOptions(bucket));
    }

    private RateLimitBucketConfig ResolveCallerBucket(HttpContext context)
    {
        if (ResolveCallerKey(context) == null)
        {
            return _config.AnonymousBucket;
        }

        var pool = ResolvePool(context);

        return ResolveMode(context) == Mode.Test
            ? ResolveBucket(_config.SandboxPools, pool)
            : ResolveBucket(_config.ClientPools, pool);
    }

    private RateLimitBucketConfig ResolveOrganizationBucket(HttpContext context)
    {
        var pool = ResolvePool(context);

        if (ResolveMode(context) == Mode.Test)
        {
            return ResolveBucket(_config.SandboxPools, pool);
        }

        var configured = ResolveBucket(_config.OrganizationPools, pool);
        var rateLimitOverride = FindOverride(context, pool);

        return rateLimitOverride == null
            ? configured
            : RateLimitOverrideSnapshot.ToBucketConfig(rateLimitOverride, configured);
    }

    private RateLimitOverride? FindOverride(HttpContext context, RateLimitPool pool)
    {
        return context.User.IsApiClient() && ResolveMode(context) == Mode.Live
            ? _rateLimitOverrideSnapshot.Find(context.User.GetOrganizationId(), pool)
            : null;
    }

    private RateLimitBucketConfig ResolveBucket(Dictionary<RateLimitPool, RateLimitBucketConfig> pools, RateLimitPool pool)
    {
        return pools.TryGetValue(pool, out var bucket) ? bucket : _config.FallbackBucket;
    }

    private RateLimitResult LocallyAllowed(HttpContext context)
    {
        var callerBucket = ResolveCallerBucket(context);
        var callerRemaining = _callerLimiter.GetStatistics(context)?.CurrentAvailablePermits ?? callerBucket.PermitLimit;

        var organizationStatistics = ResolveOrganizationKey(context) == null
            ? null
            : _organizationLimiter.GetStatistics(context);

        if (organizationStatistics == null || organizationStatistics.CurrentAvailablePermits >= callerRemaining)
        {
            return new RateLimitResult
            {
                IsAllowed = true,
                Limit = callerBucket.PermitLimit,
                Remaining = callerRemaining,
                ResetSeconds = callerBucket.ReplenishmentPeriodSeconds,
                RetryAfterSeconds = 0
            };
        }

        var organizationBucket = ResolveOrganizationBucket(context);

        return new RateLimitResult
        {
            IsAllowed = true,
            Limit = organizationBucket.PermitLimit,
            Remaining = organizationStatistics.CurrentAvailablePermits,
            ResetSeconds = organizationBucket.ReplenishmentPeriodSeconds,
            RetryAfterSeconds = 0
        };
    }
}
