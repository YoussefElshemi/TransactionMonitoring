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

public class RequestRateLimiterModeTests : BaseTestClass
{
    private readonly RateLimitOverrideSnapshot _snapshot = new();

    [Fact]
    public async Task AcquireAsync_SandboxTrafficFromTheSameClient_DoesNotSpendTheLiveBudget()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 1);
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Test));
        var sandboxExhausted = await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Test));

        // Act
        var live = await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Live));

        // Assert
        sandboxExhausted.IsAllowed.Should().BeFalse();
        live.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_SandboxTraffic_IsHeldToTheSandboxBudgetNotTheClientOne()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 100, sandboxPermitLimit: 1);
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Test));

        // Act
        var result = await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Test));

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Limit.Should().Be(1);
    }

    [Fact]
    public async Task AcquireAsync_SandboxTrafficFromTwoClients_SharesOneTenantAllowance()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 100, sandboxPermitLimit: 1);
        var organizationId = Guid.NewGuid();

        await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId, Mode.Test));

        // Act
        var result = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId, Mode.Test));

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireAsync_SandboxRequest_SendsSharedKeysCarryingTheModeAndTheOrganisationHashTag()
    {
        // Arrange
        var mockDistributedRateLimiter = new Mock<IDistributedRateLimiter>();
        RateLimitAcquireRequest? captured = null;

        mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RateLimitAcquireRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(Allowed());

        var sut = CreateSut(permitLimit: 100, distributedRateLimiter: mockDistributedRateLimiter.Object, distributedEnabled: true);
        var organizationId = Guid.NewGuid();

        // Act
        await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId, Mode.Test));

        // Assert
        captured.Should().NotBeNull();
        captured!.Buckets.Should().HaveCount(2);

        captured.Buckets.Should().OnlyContain(x => ((string)x.Key).Contains($"{{org:{organizationId}}}"));
        captured.Buckets.Should().OnlyContain(x => ((string)x.Key).EndsWith($":{Mode.Test}:{RateLimitPool.Write}"));
    }

    [Fact]
    public async Task AcquireAsync_LiveAndSandboxRequests_SendDifferentSharedKeys()
    {
        // Arrange
        var mockDistributedRateLimiter = new Mock<IDistributedRateLimiter>();
        var captured = new List<string>();

        mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RateLimitAcquireRequest, CancellationToken>(
                (request, _) => captured.AddRange(request.Buckets.Select(x => (string)x.Key)))
            .ReturnsAsync(Allowed());

        var sut = CreateSut(permitLimit: 100, distributedRateLimiter: mockDistributedRateLimiter.Object, distributedEnabled: true);
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        // Act
        await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Live));
        await sut.AcquireAsync(Context(apiClientId, organizationId, Mode.Test));

        // Assert
        captured.Should().HaveCount(4);
        captured.Distinct().Should().HaveCount(4);
    }

    [Fact]
    public async Task AcquireAsync_SignedInUser_TakesItsModeFromTheRequestHeader()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 1);
        var applicationUserId = Guid.NewGuid();

        await sut.AcquireAsync(UserContext(applicationUserId, ModeConstants.Test));
        var sandboxExhausted = await sut.AcquireAsync(UserContext(applicationUserId, ModeConstants.Test));

        // Act
        var live = await sut.AcquireAsync(UserContext(applicationUserId, ModeConstants.Live));

        // Assert
        sandboxExhausted.IsAllowed.Should().BeFalse();
        live.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_ClampingOverride_BitesLiveTrafficAndLeavesTheSandboxAlone()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _snapshot.Replace([Override(organizationId, permitLimit: 1)]);

        // A fresh client per request so only the tenant ceiling can bind.
        var sut = CreateSut(permitLimit: 500);

        await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId, Mode.Live));

        // Act
        var live = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId, Mode.Live));
        var sandbox = await sut.AcquireAsync(Context(Guid.NewGuid(), organizationId, Mode.Test));

        // Assert
        live.IsAllowed.Should().BeFalse();
        sandbox.IsAllowed.Should().BeTrue();
    }

    private static RateLimitDecision Allowed()
    {
        return new RateLimitDecision
        {
            Outcome = RateLimitOutcome.Allowed,
            PermitLimit = new PermitLimit(100),
            RemainingPermits = new RemainingPermits(99),
            RetryAfter = new RetryAfter(0)
        };
    }

    private RequestRateLimiter CreateSut(
        int permitLimit,
        int? sandboxPermitLimit = null,
        IDistributedRateLimiter? distributedRateLimiter = null,
        bool distributedEnabled = false)
    {
        var rateLimitConfig = FakeRateLimitConfig.CreateValid(permitLimit) with
        {
            Distributed = new DistributedRateLimitConfig { Enabled = distributedEnabled }
        };

        if (sandboxPermitLimit.HasValue)
        {
            var sandboxBucket = new RateLimitBucketConfig
            {
                PermitLimit = sandboxPermitLimit.Value,
                TokensPerPeriod = sandboxPermitLimit.Value,
                ReplenishmentPeriodSeconds = 300
            };

            rateLimitConfig = rateLimitConfig with
            {
                SandboxPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
                {
                    [RateLimitPool.Read] = sandboxBucket,
                    [RateLimitPool.Write] = sandboxBucket,
                    [RateLimitPool.Evaluation] = sandboxBucket
                }
            };
        }

        var appConfig = FakeAppConfig.CreateValid(Fixture) with { RateLimitConfig = rateLimitConfig };

        return new RequestRateLimiter(
            Options.Create(appConfig),
            distributedRateLimiter ?? new DisabledDistributedRateLimiter(),
            _snapshot);
    }

    private static DefaultHttpContext Context(Guid apiClientId, Guid organizationId, Mode mode)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, apiClientId.ToString()),
                new Claim(AuthConstants.ClientIdClaim, apiClientId.ToString()),
                new Claim(AuthConstants.OrganizationIdClaim, organizationId.ToString()),
                new Claim(ModeConstants.ModeClaim, mode == Mode.Test ? ModeConstants.Test : ModeConstants.Live)
            ], "Test"))
        };

        context.Request.Method = HttpMethods.Post;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        return context;
    }

    private static DefaultHttpContext UserContext(Guid applicationUserId, string mode)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, applicationUserId.ToString())
            ], "Test"))
        };

        context.Request.Method = HttpMethods.Post;
        context.Request.Headers[ModeConstants.ModeHeader] = mode;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        return context;
    }

    private static RateLimitOverride Override(Guid organizationId, int permitLimit)
    {
        var now = DateTimeOffset.UtcNow;

        return new RateLimitOverride
        {
            RateLimitOverrideId = new RateLimitOverrideId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(organizationId),
            Pool = null,
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
