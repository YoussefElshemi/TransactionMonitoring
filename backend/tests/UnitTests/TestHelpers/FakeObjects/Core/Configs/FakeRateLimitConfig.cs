using Core.Configs;
using Core.Enums;

namespace UnitTests.TestHelpers.FakeObjects.Core.Configs;

public static class FakeRateLimitConfig
{
    public static RateLimitConfig CreateValid(int permitLimit = 2, int replenishmentPeriodSeconds = 300)
    {
        var bucket = new RateLimitBucketConfig
        {
            PermitLimit = permitLimit,
            TokensPerPeriod = permitLimit,
            ReplenishmentPeriodSeconds = replenishmentPeriodSeconds,
            QueueLimit = 0
        };

        return new RateLimitConfig
        {
            Enabled = true,
            ClientPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
            {
                [RateLimitPool.Read] = bucket,
                [RateLimitPool.Write] = bucket,
                [RateLimitPool.Evaluation] = bucket
            },
            OrganizationPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
            {
                [RateLimitPool.Read] = bucket,
                [RateLimitPool.Write] = bucket,
                [RateLimitPool.Evaluation] = bucket
            },
            SandboxPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
            {
                [RateLimitPool.Read] = bucket,
                [RateLimitPool.Write] = bucket,
                [RateLimitPool.Evaluation] = bucket
            },
            AnonymousBucket = bucket,
            FallbackBucket = bucket,
            Distributed = new DistributedRateLimitConfig { Enabled = false }
        };
    }
}
