using Core.Enums;
using Core.Models.RateLimits;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Serilog;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace BehaviouralTests.Tests.RateLimiting;

public class RedisDistributedRateLimiterTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7.4-alpine").Build();

    private IConnectionMultiplexer _connectionMultiplexer = null!;
    private RedisDistributedRateLimiter _sut = null!;

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();

        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
        _sut = new RedisDistributedRateLimiter(_connectionMultiplexer, new Mock<ILogger>().Object);
    }

    public async Task DisposeAsync()
    {
        await _connectionMultiplexer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WithinBudget_AllowsAndCountsDown()
    {
        // Arrange
        var request = Request(Bucket(NewKey(), permitLimit: 3));

        // Act
        var first = await _sut.AcquireAsync(request, CancellationToken.None);
        var second = await _sut.AcquireAsync(request, CancellationToken.None);

        // Assert
        first.Outcome.Should().Be(RateLimitOutcome.Allowed);
        ((int)first.RemainingPermits!.Value).Should().Be(2);
        second.Outcome.Should().Be(RateLimitOutcome.Allowed);
        ((int)second.RemainingPermits!.Value).Should().Be(1);
    }

    [Fact]
    public async Task AcquireAsync_BudgetExhausted_RejectsWithRetryAfter()
    {
        // Arrange
        var request = Request(Bucket(NewKey(), permitLimit: 2));

        await _sut.AcquireAsync(request, CancellationToken.None);
        await _sut.AcquireAsync(request, CancellationToken.None);

        // Act
        var rejected = await _sut.AcquireAsync(request, CancellationToken.None);

        // Assert
        rejected.Outcome.Should().Be(RateLimitOutcome.Rejected);
        ((int)rejected.RemainingPermits!.Value).Should().Be(0);
        ((int)rejected.RetryAfter!.Value).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AcquireAsync_SeparateLimitersShareOneBudget_TotalSpendIsTheConfiguredLimit()
    {
        // Arrange
        var otherInstance = new RedisDistributedRateLimiter(_connectionMultiplexer, new Mock<ILogger>().Object);
        var request = Request(Bucket(NewKey(), permitLimit: 3));

        // Act
        var allowed = 0;

        foreach (var limiter in new[] { _sut, otherInstance, _sut, otherInstance, _sut })
        {
            var decision = await limiter.AcquireAsync(request, CancellationToken.None);

            if (decision.Outcome == RateLimitOutcome.Allowed)
            {
                allowed++;
            }
        }

        // Assert
        allowed.Should().Be(3);
    }

    [Fact]
    public async Task AcquireAsync_OneBucketExhausted_SpendsNeitherBucket()
    {
        // Arrange
        var callerKey = NewKey();
        var organizationKey = NewKey();

        var organizationOnly = Request(Bucket(organizationKey, permitLimit: 1));
        await _sut.AcquireAsync(organizationOnly, CancellationToken.None);

        var both = Request(Bucket(callerKey, permitLimit: 10), Bucket(organizationKey, permitLimit: 1));

        // Act
        var rejected = await _sut.AcquireAsync(both, CancellationToken.None);
        var callerAlone = await _sut.AcquireAsync(Request(Bucket(callerKey, permitLimit: 10)), CancellationToken.None);

        // Assert
        rejected.Outcome.Should().Be(RateLimitOutcome.Rejected);

        callerAlone.Outcome.Should().Be(RateLimitOutcome.Allowed);
        ((int)callerAlone.RemainingPermits!.Value).Should().Be(9);
    }

    [Fact]
    public async Task AcquireAsync_MultipleBuckets_ReportsTheMostConstrainedOne()
    {
        // Arrange
        var request = Request(Bucket(NewKey(), permitLimit: 500), Bucket(NewKey(), permitLimit: 10));

        // Act
        var decision = await _sut.AcquireAsync(request, CancellationToken.None);

        // Assert
        decision.Outcome.Should().Be(RateLimitOutcome.Allowed);
        ((int)decision.PermitLimit!.Value).Should().Be(10);
        ((int)decision.RemainingPermits!.Value).Should().Be(9);
    }

    [Fact]
    public async Task AcquireAsync_TokensReplenish_AllowsAgainAfterThePeriod()
    {
        // Arrange
        var key = NewKey();
        var request = Request(new RateLimitBucket
        {
            Key = new RateLimitKey(key),
            PermitLimit = new PermitLimit(1),
            TokensPerPeriod = new TokensPerPeriod(1),
            ReplenishmentPeriod = new ReplenishmentPeriod(1)
        });

        await _sut.AcquireAsync(request, CancellationToken.None);
        var rejected = await _sut.AcquireAsync(request, CancellationToken.None);

        // Act
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        var afterReplenishment = await _sut.AcquireAsync(request, CancellationToken.None);

        // Assert
        rejected.Outcome.Should().Be(RateLimitOutcome.Rejected);
        afterReplenishment.Outcome.Should().Be(RateLimitOutcome.Allowed);
    }

    [Fact]
    public async Task AcquireAsync_BucketIsTouched_KeyIsGivenAnExpiry()
    {
        // Arrange
        var key = NewKey();

        // Act
        await _sut.AcquireAsync(Request(Bucket(key, permitLimit: 5)), CancellationToken.None);
        var timeToLive = await _connectionMultiplexer.GetDatabase().KeyTimeToLiveAsync(key);

        // Assert
        timeToLive.Should().NotBeNull();
        timeToLive!.Value.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task AcquireAsync_StoreIsUnreachable_ReportsUnavailableRatherThanThrowing()
    {
        // Arrange
        var unreachable = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions
        {
            EndPoints = { { "127.0.0.1", 6390 } },
            AbortOnConnectFail = false,
            ConnectTimeout = 200,
            ConnectRetry = 0
        });

        var sut = new RedisDistributedRateLimiter(unreachable, new Mock<ILogger>().Object);

        // Act
        var decision = await sut.AcquireAsync(Request(Bucket(NewKey(), permitLimit: 5)), CancellationToken.None);

        // Assert
        decision.Outcome.Should().Be(RateLimitOutcome.Unavailable);
        decision.PermitLimit.Should().BeNull();

        await unreachable.DisposeAsync();
    }

    private static string NewKey()
    {
        return $"test:{Guid.NewGuid()}";
    }

    private static RateLimitBucket Bucket(string key, int permitLimit)
    {
        return new RateLimitBucket
        {
            Key = new RateLimitKey(key),
            PermitLimit = new PermitLimit(permitLimit),
            TokensPerPeriod = new TokensPerPeriod(permitLimit),
            ReplenishmentPeriod = new ReplenishmentPeriod(300)
        };
    }

    private static RateLimitAcquireRequest Request(params RateLimitBucket[] buckets)
    {
        return new RateLimitAcquireRequest { Buckets = buckets };
    }
}
