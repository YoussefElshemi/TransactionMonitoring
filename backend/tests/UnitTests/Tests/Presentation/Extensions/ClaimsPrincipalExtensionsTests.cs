using System.Security.Claims;
using AutoFixture;
using FluentAssertions;
using Presentation.Constants;
using Presentation.Extensions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Extensions;

public class ClaimsPrincipalExtensionsTests : BaseTestClass
{
    [Fact]
    public void IsApiClient_GivenOnlyAUserIdentity_ReturnFalse()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(CreateUserIdentity(Fixture.Create<Guid>()));

        // Act
        var isApiClient = claimsPrincipal.IsApiClient();

        // Assert
        isApiClient.Should().BeFalse();
    }

    [Fact]
    public void GetApiClientId_GivenAUserIdentityMergedAheadOfTheApiClientIdentity_ReturnTheApiClientId()
    {
        // Arrange
        var apiClientId = Fixture.Create<Guid>();

        var claimsPrincipal = new ClaimsPrincipal([
            CreateUserIdentity(Fixture.Create<Guid>()),
            CreateApiClientIdentity(apiClientId, Fixture.Create<Guid>(), Fixture.Create<string>())
        ]);

        // Act
        var resolvedApiClientId = claimsPrincipal.GetApiClientId();

        // Assert
        resolvedApiClientId.Should().Be(apiClientId);
    }

    [Fact]
    public void GetOrganizationId_GivenAUserIdentityMergedAheadOfTheApiClientIdentity_ReturnTheApiClientOrganizationId()
    {
        // Arrange
        var organizationId = Fixture.Create<Guid>();

        var claimsPrincipal = new ClaimsPrincipal([
            CreateUserIdentity(Fixture.Create<Guid>()),
            CreateApiClientIdentity(Fixture.Create<Guid>(), organizationId, Fixture.Create<string>())
        ]);

        // Act
        var resolvedOrganizationId = claimsPrincipal.GetOrganizationId();

        // Assert
        resolvedOrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public void HasApiScope_GivenTheScopeOnTheApiClientIdentity_ReturnTrue()
    {
        // Arrange
        var scope = Fixture.Create<string>();

        var apiClientIdentity = CreateApiClientIdentity(Fixture.Create<Guid>(), Fixture.Create<Guid>(), Fixture.Create<string>());
        apiClientIdentity.AddClaim(new Claim(AuthConstants.ScopeClaim, scope));

        var claimsPrincipal = new ClaimsPrincipal([CreateUserIdentity(Fixture.Create<Guid>()), apiClientIdentity]);

        // Act
        var hasApiScope = claimsPrincipal.HasApiScope(scope);

        // Assert
        hasApiScope.Should().BeTrue();
    }

    [Fact]
    public void HasApiScope_GivenTheScopeOnlyOnAUserIdentity_ReturnFalse()
    {
        // Arrange
        var scope = Fixture.Create<string>();

        var userIdentity = CreateUserIdentity(Fixture.Create<Guid>());
        userIdentity.AddClaim(new Claim(AuthConstants.ScopeClaim, scope));

        var claimsPrincipal = new ClaimsPrincipal([
            userIdentity,
            CreateApiClientIdentity(Fixture.Create<Guid>(), Fixture.Create<Guid>(), Fixture.Create<string>())
        ]);

        // Act
        var hasApiScope = claimsPrincipal.HasApiScope(scope);

        // Assert
        hasApiScope.Should().BeFalse();
    }

    private static ClaimsIdentity CreateUserIdentity(Guid applicationUserId)
    {
        return new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, applicationUserId.ToString())], "Cookies");
    }

    private static ClaimsIdentity CreateApiClientIdentity(Guid apiClientId, Guid organizationId, string clientId)
    {
        return new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, apiClientId.ToString()),
            new Claim(AuthConstants.ClientIdClaim, clientId),
            new Claim(AuthConstants.OrganizationIdClaim, organizationId.ToString())
        ], AuthConstants.ApiClientScheme);
    }
}
