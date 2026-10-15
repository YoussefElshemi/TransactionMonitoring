using Core.Enums;
using Core.Models.Permissions;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Models;

public class EffectivePermissionsTests : BaseTestClass
{
    private static readonly OrganizationId Organization = new(Guid.NewGuid());
    private static readonly OrganizationId OtherOrganization = new(Guid.NewGuid());
    private static readonly QueueId Queue = new(Guid.NewGuid());
    private static readonly QueueId OtherQueue = new(Guid.NewGuid());

    [Fact]
    public void Holds_GivenAScopedGrant_IsTrueWithoutAResource()
    {
        // Arrange
        var effectivePermissions = With(Grant(Permission.CasesWrite, PermissionScope.ForQueue(Queue)));

        // Act
        var holds = effectivePermissions.Holds(Permission.CasesWrite);

        // Assert
        holds.Should().BeTrue();
    }

    [Fact]
    public void Holds_GivenAPermissionNeverGranted_IsFalse()
    {
        // Arrange
        var effectivePermissions = With(Grant(Permission.CasesRead, PermissionScope.Organization()));

        // Act
        var holds = effectivePermissions.Holds(Permission.CasesDelete);

        // Assert
        holds.Should().BeFalse();
    }

    [Fact]
    public void Holds_GivenAResourceInScope_IsTrue()
    {
        // Arrange
        var effectivePermissions = With(Grant(Permission.CasesWrite, PermissionScope.ForQueue(Queue)));

        // Act
        var holds = effectivePermissions.Holds(Permission.CasesWrite, CaseIn(Organization, Queue));

        // Assert
        holds.Should().BeTrue();
    }

    [Fact]
    public void Holds_GivenAResourceOutOfScope_IsFalse()
    {
        // Arrange
        var effectivePermissions = With(Grant(Permission.CasesWrite, PermissionScope.ForQueue(Queue)));

        // Act
        var holds = effectivePermissions.Holds(Permission.CasesWrite, CaseIn(Organization, OtherQueue));

        // Assert
        holds.Should().BeFalse();
    }

    [Fact]
    public void Holds_GivenAResourceInAnotherOrganization_IsFalse()
    {
        // Arrange
        var effectivePermissions = With(Grant(Permission.CasesWrite, PermissionScope.Organization()));

        // Act
        var holds = effectivePermissions.Holds(Permission.CasesWrite, CaseIn(OtherOrganization, Queue));

        // Assert
        holds.Should().BeFalse();
    }

    [Fact]
    public void Holds_GivenTwoScopesForOnePermission_IsTrueForEither()
    {
        // Arrange
        var effectivePermissions = With(
            Grant(Permission.CasesWrite, PermissionScope.ForQueue(Queue)),
            Grant(Permission.CasesWrite, PermissionScope.ForQueue(OtherQueue)));

        // Act
        var holdsFirst = effectivePermissions.Holds(Permission.CasesWrite, CaseIn(Organization, Queue));
        var holdsSecond = effectivePermissions.Holds(Permission.CasesWrite, CaseIn(Organization, OtherQueue));

        // Assert
        holdsFirst.Should().BeTrue();
        holdsSecond.Should().BeTrue();
    }

    [Fact]
    public void IsUnscopedFor_GivenAnOrganizationGrant_IsTrue()
    {
        // Arrange
        var effectivePermissions = With(
            Grant(Permission.CasesRead, PermissionScope.ForQueue(Queue)),
            Grant(Permission.CasesRead, PermissionScope.Organization()));

        // Act
        var isUnscoped = effectivePermissions.IsUnscopedFor(Permission.CasesRead);

        // Assert
        isUnscoped.Should().BeTrue();
    }

    [Fact]
    public void IsUnscopedFor_GivenOnlyScopedGrants_IsFalse()
    {
        // Arrange
        var effectivePermissions = With(Grant(Permission.CasesRead, PermissionScope.ForQueue(Queue)));

        // Act
        var isUnscoped = effectivePermissions.IsUnscopedFor(Permission.CasesRead);

        // Assert
        isUnscoped.Should().BeFalse();
    }

    [Fact]
    public void ScopesFor_ReturnsOnlyThatPermissionsScopes()
    {
        // Arrange
        var effectivePermissions = With(
            Grant(Permission.CasesRead, PermissionScope.ForQueue(Queue)),
            Grant(Permission.CasesWrite, PermissionScope.ForQueue(OtherQueue)));

        // Act
        var scopes = effectivePermissions.ScopesFor(Permission.CasesRead);

        // Assert
        scopes.Should().ContainSingle().Which.QueueId.Should().Be(Queue);
    }

    private static PermissionGrant Grant(Permission permission, PermissionScope scope)
    {
        return new PermissionGrant { Permission = permission, Scope = scope };
    }

    private static EffectivePermissions With(params PermissionGrant[] grants)
    {
        return new EffectivePermissions
        {
            UserId = new ApplicationUserId(Guid.NewGuid()),
            OrganizationId = Organization,
            SubOrganizationId = null,
            Grants = [..grants]
        };
    }

    private static ResourceScope CaseIn(OrganizationId organizationId, QueueId queueId)
    {
        return new ResourceScope
        {
            OrganizationId = organizationId,
            SubOrganizationId = null,
            RuleSetId = null,
            QueueId = queueId
        };
    }
}
