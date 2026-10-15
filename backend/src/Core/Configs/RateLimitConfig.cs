using Core.Enums;

namespace Core.Configs;

public record RateLimitConfig
{
    public bool Enabled { get; init; } = true;

    public Dictionary<RateLimitPool, RateLimitBucketConfig> ClientPools { get; init; } = new()
    {
        [RateLimitPool.Read] = new RateLimitBucketConfig
        {
            PermitLimit = 600,
            TokensPerPeriod = 600
        },
        [RateLimitPool.Write] = new RateLimitBucketConfig
        {
            PermitLimit = 300,
            TokensPerPeriod = 300
        },
        [RateLimitPool.Evaluation] = new RateLimitBucketConfig
        {
            PermitLimit = 1_200,
            TokensPerPeriod = 1_200
        }
    };

    public Dictionary<RateLimitPool, RateLimitBucketConfig> OrganizationPools { get; init; } = new()
    {
        [RateLimitPool.Read] = new RateLimitBucketConfig
        {
            PermitLimit = 3_000,
            TokensPerPeriod = 3_000
        },
        [RateLimitPool.Write] = new RateLimitBucketConfig
        {
            PermitLimit = 1_500,
            TokensPerPeriod = 1_500
        },
        [RateLimitPool.Evaluation] = new RateLimitBucketConfig
        {
            PermitLimit = 6_000,
            TokensPerPeriod = 6_000
        }
    };

    public Dictionary<RateLimitPool, RateLimitBucketConfig> SandboxPools { get; init; } = new()
    {
        [RateLimitPool.Read] = new RateLimitBucketConfig
        {
            PermitLimit = 300,
            TokensPerPeriod = 300
        },
        [RateLimitPool.Write] = new RateLimitBucketConfig
        {
            PermitLimit = 120,
            TokensPerPeriod = 120
        },
        [RateLimitPool.Evaluation] = new RateLimitBucketConfig
        {
            PermitLimit = 120,
            TokensPerPeriod = 120
        }
    };

    public RateLimitBucketConfig AnonymousBucket { get; init; } = new()
    {
        PermitLimit = 60,
        TokensPerPeriod = 60
    };

    public RateLimitBucketConfig FallbackBucket { get; init; } = new()
    {
        PermitLimit = 60,
        TokensPerPeriod = 60
    };

    public DistributedRateLimitConfig Distributed { get; init; } = new();

    public int OverrideRefreshSeconds { get; init; } = 15;

    public int MaximumOverrideDurationDays { get; init; } = 30;
}

public record DistributedRateLimitConfig
{
    public bool Enabled { get; init; } = true;

    public bool FailOpen { get; init; } = true;

    public string KeyPrefix { get; init; } = "ratelimit";
}

public record RateLimitBucketConfig
{
    public int PermitLimit { get; init; } = 60;

    public int TokensPerPeriod { get; init; } = 60;

    public int ReplenishmentPeriodSeconds { get; init; } = 60;

    public int QueueLimit { get; init; }
}
