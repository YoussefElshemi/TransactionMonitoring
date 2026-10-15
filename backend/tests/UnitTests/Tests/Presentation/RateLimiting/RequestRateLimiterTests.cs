using System.Net;
using System.Security.Claims;
using Core.Configs;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.RateLimiting;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;

namespace UnitTests.Tests.Presentation.RateLimiting;

public class RequestRateLimiterTests : BaseTestClass
{
    [Fact]
    public async Task AcquireAsync_WithinBudget_AllowsAndReportsRemaining()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 2);
        var context = CreateApiClientContext(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var outcome = await sut.AcquireAsync(context);

        // Assert
        outcome.IsAllowed.Should().BeTrue();
        outcome.Limit.Should().Be(2);
        outcome.Remaining.Should().Be(1);
        outcome.RetryAfterSeconds.Should().Be(0);
    }

    [Fact]
    public async Task AcquireAsync_BudgetExhausted_RejectsWithRetryAfter()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 2);
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId));
        await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId));

        // Act
        var outcome = await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId));

        // Assert
        outcome.IsAllowed.Should().BeFalse();
        outcome.Remaining.Should().Be(0);
        outcome.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AcquireAsync_EvaluationBudgetExhausted_StillAllowsReads()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 1);
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId, pool: RateLimitPool.Evaluation));
        var exhausted = await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId, pool: RateLimitPool.Evaluation));

        // Act
        var read = await sut.AcquireAsync(CreateApiClientContext(apiClientId, organizationId, method: HttpMethods.Get));

        // Assert
        exhausted.IsAllowed.Should().BeFalse();
        read.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_SeparateApiClients_DoNotShareCallerBudget()
    {
        // Arrange
        var organizationBucket = new RateLimitBucketConfig
        {
            PermitLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriodSeconds = 300
        };

        var sut = CreateSut(FakeRateLimitConfig.CreateValid(permitLimit: 1) with
        {
            OrganizationPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
            {
                [RateLimitPool.Read] = organizationBucket,
                [RateLimitPool.Write] = organizationBucket,
                [RateLimitPool.Evaluation] = organizationBucket
            }
        });

        var organizationId = Guid.NewGuid();
        await sut.AcquireAsync(CreateApiClientContext(Guid.NewGuid(), organizationId));

        // Act
        var outcome = await sut.AcquireAsync(CreateApiClientContext(Guid.NewGuid(), organizationId));

        // Assert
        outcome.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_OrganizationCeilingReached_RejectsClientWithBudgetRemaining()
    {
        // Arrange
        var config = FakeRateLimitConfig.CreateValid(permitLimit: 5);
        var organizationBucket = new RateLimitBucketConfig
        {
            PermitLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriodSeconds = 300
        };

        var sut = CreateSut(config with
        {
            OrganizationPools = new Dictionary<RateLimitPool, RateLimitBucketConfig>
            {
                [RateLimitPool.Read] = organizationBucket,
                [RateLimitPool.Write] = organizationBucket,
                [RateLimitPool.Evaluation] = organizationBucket
            }
        });

        var organizationId = Guid.NewGuid();
        await sut.AcquireAsync(CreateApiClientContext(Guid.NewGuid(), organizationId));

        // Act
        var outcome = await sut.AcquireAsync(CreateApiClientContext(Guid.NewGuid(), organizationId));

        // Assert
        outcome.IsAllowed.Should().BeFalse();
        outcome.Limit.Should().Be(1);
    }

    [Fact]
    public async Task AcquireAsync_UnauthenticatedRequests_ArePartitionedByAddress()
    {
        // Arrange
        var sut = CreateSut(permitLimit: 1);

        await sut.AcquireAsync(CreateAnonymousContext(IPAddress.Parse("203.0.113.1")));

        // Act
        var sameAddress = await sut.AcquireAsync(CreateAnonymousContext(IPAddress.Parse("203.0.113.1")));
        var otherAddress = await sut.AcquireAsync(CreateAnonymousContext(IPAddress.Parse("203.0.113.2")));

        // Assert
        sameAddress.IsAllowed.Should().BeFalse();
        otherAddress.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void IsExempt_EndpointCarriesExemption_ReturnsTrue()
    {
        // Arrange
        var context = CreateAnonymousContext(IPAddress.Loopback);
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RateLimitExemptAttribute()),
            null));

        // Act
        var isExempt = RequestRateLimiter.IsExempt(context);

        // Assert
        isExempt.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ConfigurationDisablesLimiting_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut(FakeRateLimitConfig.CreateValid() with { Enabled = false });

        // Act
        var isEnabled = sut.IsEnabled;

        // Assert
        isEnabled.Should().BeFalse();
    }

    private RequestRateLimiter CreateSut(int permitLimit)
    {
        return CreateSut(FakeRateLimitConfig.CreateValid(permitLimit));
    }

    private RequestRateLimiter CreateSut(RateLimitConfig rateLimitConfig)
    {
        var appConfig = FakeAppConfig.CreateValid(Fixture) with { RateLimitConfig = rateLimitConfig };

        return new RequestRateLimiter(Options.Create(appConfig), new DisabledDistributedRateLimiter(), new RateLimitOverrideSnapshot());
    }

    private static DefaultHttpContext CreateApiClientContext(
        Guid apiClientId,
        Guid organizationId,
        RateLimitPool? pool = null,
        string? method = null)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, apiClientId.ToString()),
                new Claim(AuthConstants.ClientIdClaim, apiClientId.ToString()),
                new Claim(AuthConstants.OrganizationIdClaim, organizationId.ToString())
            ], "Test"))
        };

        context.Request.Method = method ?? HttpMethods.Post;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        if (pool.HasValue)
        {
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new RateLimitAttribute(pool.Value)),
                null));
        }

        return context;
    }

    private static DefaultHttpContext CreateAnonymousContext(IPAddress remoteIpAddress)
    {
        var context = new DefaultHttpContext();

        context.Request.Method = HttpMethods.Post;
        context.Connection.RemoteIpAddress = remoteIpAddress;

        return context;
    }
}
