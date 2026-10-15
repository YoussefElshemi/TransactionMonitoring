using System.Net;
using System.Security.Claims;
using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.RateLimitOverrides;
using Core.Models.RateLimits;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Presentation.Constants;
using Presentation.RateLimiting;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;

namespace UnitTests.Tests.Presentation.RateLimiting;

public class RequestRateLimiterOverrideTests : BaseTestClass
{
    private readonly RateLimitOverrideSnapshot _snapshot = new();

    [Fact]
    public async Task AcquireAsync_ClampingOverride_TakesEffectOnTheLocalCeiling()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _snapshot.Replace([Override(organizationId, pool: null, permitLimit: 1)]);

        var sut = CreateSut(organizationPermitLimit: 500);

        // Act
        var first = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));
        var second = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));

        // Assert
        first.IsAllowed.Should().BeTrue();
        second.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireAsync_RaisingOverride_ReportsTheRaisedCeiling()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _snapshot.Replace([Override(organizationId, pool: null, permitLimit: 9_000)]);

        var sut = CreateSut(organizationPermitLimit: 2, clientPermitLimit: 20_000);

        // Act
        var result = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Limit.Should().Be(9_000);
    }

    [Fact]
    public async Task AcquireAsync_OverrideForAnotherTenant_LeavesThisOneOnTheConfiguredCeiling()
    {
        // Arrange
        _snapshot.Replace([Override(Guid.NewGuid(), pool: null, permitLimit: 1)]);

        var sut = CreateSut(organizationPermitLimit: 500);
        var organizationId = Guid.NewGuid();

        // Act
        var first = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));
        var second = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));

        // Assert
        first.IsAllowed.Should().BeTrue();
        second.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_OverrideApplied_IsSentToTheSharedStoreToo()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _snapshot.Replace([Override(organizationId, pool: null, permitLimit: 42)]);

        var mockDistributedRateLimiter = new Mock<IDistributedRateLimiter>();
        RateLimitAcquireRequest? captured = null;

        mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RateLimitAcquireRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new RateLimitDecision
            {
                Outcome = RateLimitOutcome.Allowed,
                PermitLimit = new PermitLimit(42),
                RemainingPermits = new RemainingPermits(41),
                RetryAfter = new RetryAfter(0)
            });

        var sut = CreateSut(organizationPermitLimit: 500, distributedRateLimiter: mockDistributedRateLimiter.Object, distributedEnabled: true);

        // Act
        await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));

        // Assert
        captured.Should().NotBeNull();
        captured!.Buckets.Should().HaveCount(2);
        ((int)captured.Buckets[1].PermitLimit).Should().Be(42);
    }

    [Fact]
    public async Task AcquireAsync_OverrideLifted_ReturnsToTheConfiguredCeiling()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _snapshot.Replace([Override(organizationId, pool: null, permitLimit: 1)]);

        var sut = CreateSut(organizationPermitLimit: 500);

        await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));
        var whileClamped = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));

        // Act
        _snapshot.Replace([]);
        var afterLifting = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId));

        // Assert
        whileClamped.IsAllowed.Should().BeFalse();
        afterLifting.IsAllowed.Should().BeTrue();
    }

    private RequestRateLimiter CreateSut(
        int organizationPermitLimit,
        IDistributedRateLimiter? distributedRateLimiter = null,
        bool distributedEnabled = false,
        int clientPermitLimit = 500)
    {
        var organizationBucket = new RateLimitBucketConfig
        {
            PermitLimit = organizationPermitLimit,
            TokensPerPeriod = organizationPermitLimit,
            ReplenishmentPeriodSeconds = 300
        };

        var rateLimitConfig = FakeRateLimitConfig.CreateValid(permitLimit: clientPermitLimit) with
        {
            OrganizationPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
            {
                [RateLimitPool.Read] = organizationBucket,
                [RateLimitPool.Write] = organizationBucket,
                [RateLimitPool.Evaluation] = organizationBucket
            },
            Distributed = new DistributedRateLimitConfig { Enabled = distributedEnabled }
        };

        var appConfig = FakeAppConfig.CreateValid(Fixture) with { RateLimitConfig = rateLimitConfig };

        return new RequestRateLimiter(
            Options.Create(appConfig),
            distributedRateLimiter ?? new DisabledDistributedRateLimiter(),
            _snapshot);
    }

    private static DefaultHttpContext Context(Guid apiClientId, Guid organizationId)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, apiClientId.ToString()),
                new Claim(AuthConstants.ClientIdClaim, apiClientId.ToString()),
                new Claim(AuthConstants.OrganizationIdClaim, organizationId.ToString())
            ], "Test"))
        };

        context.Request.Method = HttpMethods.Post;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        return context;
    }

    private static RateLimitOverride Override(Guid organizationId, RateLimitPool? pool, int permitLimit)
    {
        var now = DateTimeOffset.UtcNow;

        return new RateLimitOverride
        {
            RateLimitOverrideId = new RateLimitOverrideId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(organizationId),
            Pool = pool,
            PermitLimit = new PermitLimit(permitLimit),
            TokensPerPeriod = new TokensPerPeriod(permitLimit),
            ReplenishmentPeriod = new ReplenishmentPeriod(300),
            Reason = new RateLimitOverrideReason("Incident 4021"),
            ExpiresAt = new ExpiresAt(now.AddHours(1)),
            CreatedByUserId = new ApplicationUserId(Guid.NewGuid()),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
