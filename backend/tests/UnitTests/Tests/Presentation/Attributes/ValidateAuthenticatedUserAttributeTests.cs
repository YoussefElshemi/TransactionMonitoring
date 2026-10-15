using System.Security.Claims;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.Permissions;
using Core.Models.Users;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Presentation.Attributes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;

namespace UnitTests.Tests.Presentation.Attributes;

public class ValidateAuthenticatedUserAttributeTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _applicationUserServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly ValidateAuthenticatedUserAttribute _attribute = new();

    [Fact]
    public async Task OnActionExecutionAsync_ValidUser_ShouldCallNext()
    {
        // Arrange
        GivenAuthenticatedUser(FakeApplicationUser.CreateValid(Fixture));

        var context = CreateActionExecutingContext();
        var nextMock = new Mock<ActionExecutionDelegate>();
        nextMock.Setup(n => n()).Returns(Task.FromResult<ActionExecutedContext>(null!));

        // Act
        await _attribute.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        nextMock.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task OnActionExecutionAsync_UserBelongsToDefaultOrganization_ThrowsException()
    {
        // Arrange
        GivenAuthenticatedUser(CreateDefaultOrganizationUser());

        var context = CreateActionExecutingContext();

        // Act
        await Assert.ThrowsAsync<ValidationException>(() =>
            _attribute.OnActionExecutionAsync(context, Mock.Of<ActionExecutionDelegate>())
        );
    }

    [Fact]
    public async Task OnActionExecutionAsync_OrganizationIsNotEnabled_ThrowsException()
    {
        // Arrange
        GivenAuthenticatedUser(FakeApplicationUser.CreateValid(Fixture) with
        {
            Organization = FakeOrganization.CreateValid(Fixture) with
            {
                Status = OrganizationStatus.Disabled
            }
        });

        var context = CreateActionExecutingContext();

        // Act
        await Assert.ThrowsAsync<ValidationException>(() =>
            _attribute.OnActionExecutionAsync(context, Mock.Of<ActionExecutionDelegate>())
        );
    }

    [Fact]
    public async Task OnActionExecutionAsync_PlatformAdministratorInDefaultOrganizationOnExemptEndpoint_ShouldCallNext()
    {
        // Arrange
        var user = CreateDefaultOrganizationUser();
        GivenAuthenticatedUser(user);
        GivenUserHolds(user, UserRole.Admin);

        var context = CreateActionExecutingContext(
            endpointMetadata: [new OrganizationScopeExemptAttribute("Test")]);

        var nextMock = new Mock<ActionExecutionDelegate>();
        nextMock.Setup(n => n()).Returns(Task.FromResult<ActionExecutedContext>(null!));

        // Act
        await _attribute.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        nextMock.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task OnActionExecutionAsync_PlatformAdministratorInDefaultOrganizationOnRouteNamingOrganization_ShouldCallNext()
    {
        // Arrange
        var user = CreateDefaultOrganizationUser();
        GivenAuthenticatedUser(user);
        GivenUserHolds(user, UserRole.Admin);

        var context = CreateActionExecutingContext(routeOrganizationId: Guid.NewGuid().ToString());

        var nextMock = new Mock<ActionExecutionDelegate>();
        nextMock.Setup(n => n()).Returns(Task.FromResult<ActionExecutedContext>(null!));

        // Act
        await _attribute.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        nextMock.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task OnActionExecutionAsync_PlatformAdministratorInDefaultOrganizationOnMyRoute_ThrowsException()
    {
        // Arrange
        var user = CreateDefaultOrganizationUser();
        GivenAuthenticatedUser(user);
        GivenUserHolds(user, UserRole.Admin);

        var context = CreateActionExecutingContext(routeOrganizationId: "my");

        // Act
        await Assert.ThrowsAsync<ValidationException>(() =>
            _attribute.OnActionExecutionAsync(context, Mock.Of<ActionExecutionDelegate>())
        );
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Auditor)]
    public async Task OnActionExecutionAsync_NonPlatformAdministratorInDefaultOrganizationOnExemptEndpoint_ThrowsException(UserRole userRole)
    {
        // Arrange
        var user = CreateDefaultOrganizationUser();
        GivenAuthenticatedUser(user);
        GivenUserHolds(user, userRole);

        var context = CreateActionExecutingContext(
            endpointMetadata: [new OrganizationScopeExemptAttribute("Test")]);

        // Act
        await Assert.ThrowsAsync<ValidationException>(() =>
            _attribute.OnActionExecutionAsync(context, Mock.Of<ActionExecutionDelegate>())
        );
    }

    private ApplicationUser CreateDefaultOrganizationUser()
    {
        return FakeApplicationUser.CreateValid(Fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization)
        };
    }

    private void GivenAuthenticatedUser(ApplicationUser applicationUser)
    {
        _applicationUserServiceMock
            .Setup(s => s.GetAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicationUser);
    }

    private void GivenUserHolds(ApplicationUser applicationUser, UserRole userRole)
    {
        _permissionServiceMock
            .Setup(s => s.ResolveAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EffectivePermissions
            {
                UserId = applicationUser.UserId,
                OrganizationId = applicationUser.OrganizationId,
                SubOrganizationId = null,
                Grants = PermissionBundle.For(userRole)
                    .Select(x => new PermissionGrant { Permission = x, Scope = PermissionScope.Organization() })
                    .ToList()
            });
    }

    private ActionExecutingContext CreateActionExecutingContext(
        object[]? endpointMetadata = null,
        string? routeOrganizationId = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "TestUser")])),
            RequestServices = Mock.Of<IServiceProvider>(sp =>
                sp.GetService(typeof(IApplicationUserService)) == _applicationUserServiceMock.Object &&
                sp.GetService(typeof(IPermissionService)) == _permissionServiceMock.Object)
        };

        var routeData = new RouteData();

        if (routeOrganizationId is not null)
        {
            routeData.Values["OrganizationId"] = routeOrganizationId;
        }

        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = routeData,
            ActionDescriptor = new ControllerActionDescriptor
            {
                EndpointMetadata = endpointMetadata ?? []
            }
        };

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }
}
