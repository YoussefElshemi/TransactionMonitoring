using AutoFixture;
using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Infrastructure.Entities;

public class AuditedRelationRegistryTests : BaseTestClass
{
    [Fact]
    public void TryGetOwner_GivenAnApiClientScope_ReturnsTheApiClient()
    {
        // Arrange
        var apiClientId = Fixture.Create<Guid>();
        var apiClientScopeEntity = new ApiClientScopeEntity
        {
            ApiClientId = apiClientId,
            ApiScopeId = (int)ApiScope.RulesEvaluate,
            CreatedAt = Fixture.Create<DateTimeOffset>(),
            UpdatedAt = Fixture.Create<DateTimeOffset>()
        };

        // Act
        var found = AuditedRelationRegistry.TryGetOwner(apiClientScopeEntity, out var owner);

        // Assert
        found.Should().BeTrue();
        owner.Should().Be(new AuditedEntityKey(AuditableEntityType.ApiClient, apiClientId));
    }

    [Fact]
    public void TryGetOwner_GivenAUserRole_ReturnsTheUser()
    {
        // Arrange
        var userId = Fixture.Create<Guid>();
        var userRole = new IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = RoleHelper.MapFromUserRole(UserRole.Manager)
        };

        // Act
        var found = AuditedRelationRegistry.TryGetOwner(userRole, out var owner);

        // Assert
        found.Should().BeTrue();
        owner.Should().Be(new AuditedEntityKey(AuditableEntityType.ApplicationUser, userId));
    }

    [Fact]
    public void TryGetOwner_GivenAnEntityThatIsNotARelation_ReturnsFalse()
    {
        // Act
        var found = AuditedRelationRegistry.TryGetOwner(new ApiScopeEntity
        {
            ApiScopeId = (int)ApiScope.RulesEvaluate,
            Name = ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate),
            CreatedAt = Fixture.Create<DateTimeOffset>(),
            UpdatedAt = Fixture.Create<DateTimeOffset>()
        }, out var owner);

        // Assert
        found.Should().BeFalse();
        owner.Should().Be(default(AuditedEntityKey));
    }
}
