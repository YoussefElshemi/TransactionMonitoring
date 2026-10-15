using System.Net;
using System.Security.Claims;
using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.RateLimits;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Presentation.Constants;
using Presentation.RateLimiting;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;

namespace UnitTests.Tests.Presentation.RateLimiting;

public class RequestRateLimiterDistributedTests : BaseTestClass
{
    private readonly Mock<IDistributedRateLimiter> _mockDistributedRateLimiter = new();

    [Fact]
    public async Task AcquireAsync_SharedStoreAllows_ReportsSharedFiguresNotLocalOnes()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Allowed, permitLimit: 5_000, remainingPermits: 4_321);
        var sut = CreateSut();

        // Act
        var result = await sut.AcquireAsync(CreateApiClientContext());

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Limit.Should().Be(5_000);
        result.Remaining.Should().Be(4_321);
    }

    [Fact]
    public async Task AcquireAsync_SharedStoreRejects_RejectsEvenThoughLocalBudgetRemains()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Rejected, permitLimit: 5_000, remainingPermits: 0, retryAfter: 12);
        var sut = CreateSut();

        // Act
        var result = await sut.AcquireAsync(CreateApiClientContext());

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RetryAfterSeconds.Should().Be(12);
    }

    [Fact]
    public async Task AcquireAsync_SharedStoreUnavailableAndFailOpen_AllowsOnLocalBudget()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Unavailable);
        var sut = CreateSut();

        // Act
        var result = await sut.AcquireAsync(CreateApiClientContext());

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Limit.Should().Be(2);
    }

    [Fact]
    public async Task AcquireAsync_SharedStoreUnavailableAndFailClosed_Rejects()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Unavailable);
        var sut = CreateSut(failOpen: false);

        // Act
        var result = await sut.AcquireAsync(CreateApiClientContext());

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AcquireAsync_LocalBudgetExhausted_DoesNotReachSharedStore()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Allowed, permitLimit: 5_000, remainingPermits: 4_999);
        var sut = CreateSut();
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId));
        await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId));
        _mockDistributedRateLimiter.Invocations.Clear();

        // Act
        var result = await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId));

        // Assert
        result.IsAllowed.Should().BeFalse();
        _mockDistributedRateLimiter.Verify(
            x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcquireAsync_ApiClientRequest_SendsCallerAndOrganisationBucketsSharingOneHashTag()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Allowed, permitLimit: 5_000, remainingPermits: 4_999);
        var sut = CreateSut();
        var organizationId = Guid.NewGuid();

        RateLimitAcquireRequest? captured = null;
        _mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RateLimitAcquireRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(Decision(RateLimitOutcome.Allowed, 5_000, 4_999, 0));

        // Act
        await sut.AcquireAsync(CreateApiClientContext(Guid.NewGuid(), organizationId));

        // Assert
        captured.Should().NotBeNull();
        captured!.Buckets.Should().HaveCount(2);

        var hashTag = $"{{org:{organizationId}}}";
        captured.Buckets.Should().OnlyContain(x => ((string)x.Key).Contains(hashTag));
    }

    [Fact]
    public async Task AcquireAsync_UnauthenticatedRequest_SendsSingleAnonymousBucket()
    {
        // Arrange
        SetUpDecision(RateLimitOutcome.Allowed, permitLimit: 60, remainingPermits: 59);
        var sut = CreateSut();

        RateLimitAcquireRequest? captured = null;
        _mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RateLimitAcquireRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(Decision(RateLimitOutcome.Allowed, 60, 59, 0));

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        // Act
        await sut.AcquireAsync(context);

        // Assert
        captured.Should().NotBeNull();
        captured!.Buckets.Should().ContainSingle();
        ((string)captured.Buckets[0].Key).Should().Contain("203.0.113.7");
    }

    private void SetUpDecision(
        RateLimitOutcome outcome,
        int permitLimit = 1,
        int remainingPermits = 0,
        int retryAfter = 0)
    {
        _mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Decision(outcome, permitLimit, remainingPermits, retryAfter));
    }

    private static RateLimitDecision Decision(
        RateLimitOutcome outcome,
        int permitLimit,
        int remainingPermits,
        int retryAfter)
    {
        var isUnavailable = outcome == RateLimitOutcome.Unavailable;

        return new RateLimitDecision
        {
            Outcome = outcome,
            PermitLimit = isUnavailable ? null : new PermitLimit(permitLimit),
            RemainingPermits = isUnavailable ? null : new RemainingPermits(remainingPermits),
            RetryAfter = isUnavailable ? null : new RetryAfter(retryAfter)
        };
    }

    private RequestRateLimiter CreateSut(bool failOpen = true)
    {
        var rateLimitConfig = FakeRateLimitConfig.CreateValid(permitLimit: 2) with
        {
            Distributed = new DistributedRateLimitConfig { Enabled = true, FailOpen = failOpen }
        };

        var appConfig = FakeAppConfig.CreateValid(Fixture) with { RateLimitConfig = rateLimitConfig };

        return new RequestRateLimiter(Options.Create(appConfig), _mockDistributedRateLimiter.Object, new RateLimitOverrideSnapshot());
    }

    private static DefaultHttpContext CreateApiClientContext(Guid? apiClientId = null, Guid? organizationId = null)
    {
        var resolvedApiClientId = apiClientId ?? Guid.NewGuid();

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, resolvedApiClientId.ToString()),
                new Claim(AuthConstants.ClientIdClaim, resolvedApiClientId.ToString()),
                new Claim(AuthConstants.OrganizationIdClaim, (organizationId ?? Guid.NewGuid()).ToString())
            ], "Test"))
        };

        context.Request.Method = HttpMethods.Post;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        return context;
    }
}
