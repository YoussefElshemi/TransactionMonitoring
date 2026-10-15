using System.Security.Claims;
using AutoFixture;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Presentation.Constants;
using Presentation.Middlewares;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Middlewares;

public class EnsureUserExistsMiddlewareTests : BaseTestClass
{
    private readonly DefaultHttpContext _context = new();
    private readonly Mock<IApiClientService> _mockApiClientService = new();
    private readonly Mock<IApplicationUserService> _mockApplicationUserService = new();
    private readonly EnsureUserExistsMiddleware _sut = new(_ => Task.CompletedTask);

    public EnsureUserExistsMiddlewareTests()
    {
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute()),
            null);

        _context.SetEndpoint(endpoint);
    }

    [Fact]
    public async Task InvokeAsync_UserDoesNotExist_ReturnUnauthorized()
    {
        // Arrange
        var userId = Fixture.Create<Guid>().ToString();
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "Test"));

        _mockApplicationUserService
            .Setup(service => service.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        await _sut.InvokeAsync(_context, _mockApplicationUserService.Object, _mockApiClientService.Object);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_UserDoesExist_CallNextMiddleware()
    {
        // Arrange
        var userId = Fixture.Create<Guid>().ToString();
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "Test"));

        _mockApplicationUserService
            .Setup(service => service.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        var nextMock = new Mock<RequestDelegate>();
        nextMock
            .Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new EnsureUserExistsMiddleware(nextMock.Object);

        // Act
        await middleware.InvokeAsync(_context, _mockApplicationUserService.Object, _mockApiClientService.Object);

        // Assert
        nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_EndpointDoesNotRequireAuthorization_CallNextMiddleware()
    {
        // Arrange
        _context.SetEndpoint(null);

        var nextMock = new Mock<RequestDelegate>();
        nextMock
            .Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new EnsureUserExistsMiddleware(nextMock.Object);

        // Act
        await middleware.InvokeAsync(_context, _mockApplicationUserService.Object, _mockApiClientService.Object);

        // Assert
        nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UserIsUnauthenticated_ReturnUnauthorized()
    {
        // Arrange
        _context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var nextMock = new Mock<RequestDelegate>();
        nextMock
            .Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new EnsureUserExistsMiddleware(nextMock.Object);

        // Act
        await middleware.InvokeAsync(_context, _mockApplicationUserService.Object, _mockApiClientService.Object);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_ApiClientIdentityAlongsideAUserIdentity_ChecksTheApiClientId()
    {
        // Arrange
        var apiClientId = Fixture.Create<Guid>();

        var userIdentity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Fixture.Create<Guid>().ToString())
        ], "Cookies");

        var apiClientIdentity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, apiClientId.ToString()),
            new Claim(AuthConstants.ClientIdClaim, Fixture.Create<string>())
        ], AuthConstants.ApiClientScheme);

        _context.User = new ClaimsPrincipal([userIdentity, apiClientIdentity]);

        _mockApiClientService
            .Setup(service => service.IsActiveAsync(new ApiClientId(apiClientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var middleware = new EnsureUserExistsMiddleware(nextMock.Object);

        // Act
        await middleware.InvokeAsync(_context, _mockApplicationUserService.Object, _mockApiClientService.Object);

        // Assert
        nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
        _mockApiClientService.Verify(service => service.IsActiveAsync(new ApiClientId(apiClientId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UserIdMalformed_ReturnUnauthorized()
    {
        // Arrange
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
        ], "Test"));

        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var middleware = new EnsureUserExistsMiddleware(nextMock.Object);

        // Act
        await middleware.InvokeAsync(_context, _mockApplicationUserService.Object, _mockApiClientService.Object);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}