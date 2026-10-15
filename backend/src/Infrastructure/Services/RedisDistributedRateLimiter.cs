using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.RateLimits;
using Core.ValueObjects;
using Serilog;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class RedisDistributedRateLimiter(IConnectionMultiplexer connectionMultiplexer, ILogger logger) : IDistributedRateLimiter
{
    // Under Redis's clock: instances' clocks don't agree closely enough to share a bucket.
    private const string AcquireScript =
        """
        local now = redis.call('TIME')
        local now_ms = tonumber(now[1]) * 1000 + math.floor(tonumber(now[2]) / 1000)

        local allowed = 1
        local retry_after_ms = 0
        local tokens = {}

        for i = 1, #KEYS do
            local base = (i - 1) * 3
            local capacity = tonumber(ARGV[base + 1])
            local per_period = tonumber(ARGV[base + 2])
            local period_ms = tonumber(ARGV[base + 3])

            local state = redis.call('HMGET', KEYS[i], 'tokens', 'timestamp')
            local available = tonumber(state[1])
            local last = tonumber(state[2])

            if available == nil or last == nil then
                available = capacity
                last = now_ms
            end

            local elapsed = now_ms - last

            if elapsed < 0 then
                elapsed = 0
            end

            available = math.min(capacity, available + (elapsed / period_ms) * per_period)

            if available < 1 then
                allowed = 0
                local wait = math.ceil(((1 - available) / per_period) * period_ms)

                if wait > retry_after_ms then
                    retry_after_ms = wait
                end
            end

            tokens[i] = available
        end

        local limit = -1
        local remaining = -1

        for i = 1, #KEYS do
            local base = (i - 1) * 3
            local capacity = tonumber(ARGV[base + 1])
            local per_period = tonumber(ARGV[base + 2])
            local period_ms = tonumber(ARGV[base + 3])
            local available = tokens[i]

            if allowed == 1 then
                available = available - 1
            end

            redis.call('HSET', KEYS[i], 'tokens', tostring(available), 'timestamp', tostring(now_ms))

            -- Long enough for an untouched bucket to refill completely, so an idle tenant's key expires
            -- instead of accumulating forever, and a returning one is indistinguishable from full.
            local ttl = math.ceil((capacity / per_period) * (period_ms / 1000)) + 60
            redis.call('EXPIRE', KEYS[i], ttl)

            local floored = math.floor(available)

            if remaining < 0 or floored < remaining then
                remaining = floored
                limit = capacity
            end
        end

        if remaining < 0 then
            remaining = 0
        end

        return { allowed, limit, remaining, retry_after_ms }
        """;

    public async Task<RateLimitDecision> AcquireAsync(
        RateLimitAcquireRequest rateLimitAcquireRequest,
        CancellationToken stoppingToken)
    {
        if (rateLimitAcquireRequest.Buckets.Count == 0)
        {
            return Unavailable();
        }

        var keys = rateLimitAcquireRequest.Buckets
            .Select(x => (RedisKey)(string)x.Key)
            .ToArray();

        var values = rateLimitAcquireRequest.Buckets
            .SelectMany(x => new RedisValue[]
            {
                (int)x.PermitLimit,
                (int)x.TokensPerPeriod,
                x.ReplenishmentPeriod * 1000
            })
            .ToArray();

        try
        {
            stoppingToken.ThrowIfCancellationRequested();

            var result = (RedisResult[]?)await connectionMultiplexer
                .GetDatabase()
                .ScriptEvaluateAsync(AcquireScript, keys, values);

            if (result is not { Length: 4 })
            {
                return Unavailable();
            }

            var retryAfterMilliseconds = (long)result[3];

            return new RateLimitDecision
            {
                Outcome = (int)result[0] == 1 ? RateLimitOutcome.Allowed : RateLimitOutcome.Rejected,
                PermitLimit = new PermitLimit((int)result[1]),
                RemainingPermits = new RemainingPermits((int)result[2]),
                RetryAfter = new RetryAfter((int)Math.Ceiling(retryAfterMilliseconds / 1000d))
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Warning(exception, "Distributed rate limit check failed: {Message}", exception.Message);

            return Unavailable();
        }
    }

    private static RateLimitDecision Unavailable()
    {
        return new RateLimitDecision
        {
            Outcome = RateLimitOutcome.Unavailable,
            PermitLimit = null,
            RemainingPermits = null,
            RetryAfter = null
        };
    }
}
