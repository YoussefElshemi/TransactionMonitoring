using System.Net;
using System.Security.Claims;
using Core.Configs;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Middlewares;
using Presentation.RateLimiting;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;

namespace UnitTests.Tests.Presentation.Middlewares;

public class RateLimitMiddlewareTests : BaseTestClass
{
    private bool _nextWasCalled;

    [Fact]
    public async Task InvokeAsync_WithinBudget_CallsNextAndWritesRateLimitHeaders()
    {
        // Arrange
        var sut = CreateSut(FakeRateLimitConfig.CreateValid(permitLimit: 2));
        var context = CreateApiClientContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        context.Response.Headers[HeaderNames.RateLimitLimit].ToString().Should().Be("2");
        context.Response.Headers[HeaderNames.RateLimitRemaining].ToString().Should().Be("1");
        context.Response.Headers[HeaderNames.RateLimitReset].ToString().Should().Be("300");
        context.Response.Headers.Should().NotContainKey(HeaderNames.RetryAfter);
    }

    [Fact]
    public async Task InvokeAsync_BudgetExhausted_ReturnsTooManyRequestsWithRetryAfter()
    {
        // Arrange
        var sut = CreateSut(FakeRateLimitConfig.CreateValid(permitLimit: 1));
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.InvokeAsync(CreateApiClientContext(apiClientId, organizationId));
        _nextWasCalled = false;

        var context = CreateApiClientContext(apiClientId, organizationId);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.Headers[HeaderNames.RetryAfter].ToString().Should().NotBeNullOrEmpty();
        context.Response.Headers[HeaderNames.RateLimitRemaining].ToString().Should().Be("0");
    }

    [Fact]
    public async Task InvokeAsync_EndpointIsExempt_CallsNextWithoutLimiting()
    {
        // Arrange
        var sut = CreateSut(FakeRateLimitConfig.CreateValid(permitLimit: 1));
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.InvokeAsync(CreateExemptContext(apiClientId, organizationId));
        _nextWasCalled = false;

        var context = CreateExemptContext(apiClientId, organizationId);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Headers.Should().NotContainKey(HeaderNames.RateLimitLimit);
    }

    [Fact]
    public async Task InvokeAsync_LimitingDisabled_CallsNextWithoutLimiting()
    {
        // Arrange
        var sut = CreateSut(FakeRateLimitConfig.CreateValid(permitLimit: 1) with { Enabled = false });
        var apiClientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await sut.InvokeAsync(CreateApiClientContext(apiClientId, organizationId));
        _nextWasCalled = false;

        var context = CreateApiClientContext(apiClientId, organizationId);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        context.Response.Headers.Should().NotContainKey(HeaderNames.RateLimitLimit);
    }

    private RateLimitMiddleware CreateSut(RateLimitConfig rateLimitConfig)
    {
        var appConfig = FakeAppConfig.CreateValid(Fixture) with { RateLimitConfig = rateLimitConfig };

        return new RateLimitMiddleware(
            _ =>
            {
                _nextWasCalled = true;

                return Task.CompletedTask;
            },
            new RequestRateLimiter(Options.Create(appConfig), new DisabledDistributedRateLimiter(), new RateLimitOverrideSnapshot()));
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

    private static DefaultHttpContext CreateExemptContext(Guid apiClientId, Guid organizationId)
    {
        var context = CreateApiClientContext(apiClientId, organizationId);

        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RateLimitExemptAttribute()),
            null));

        return context;
    }
}
