using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PermissionBundleTests : BaseTestClass
{
    [Fact]
    public void For_ForEveryBuiltInRole_HasABundle()
    {
        // Arrange
        var roles = Enum.GetValues<UserRole>();

        // Act
        var act = () => roles.Select(PermissionBundle.For).ToList();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void For_GivenAdmin_HoldsEveryPermission()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.Admin);

        // Assert
        permissions.Should().BeEquivalentTo(Enum.GetValues<Permission>());
    }

    [Fact]
    public void For_GivenManager_HoldsEverythingTheDeskDoes()
    {
        // Act
        var user = PermissionBundle.For(UserRole.User);
        var manager = PermissionBundle.For(UserRole.Manager);

        // Assert
        manager.Should().Contain(user);
    }

    [Fact]
    public void For_GivenUser_DoesNotHoldThePlatformPermission()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.User);

        // Assert
        permissions.Should().NotContain(Permission.PlatformAdminister);
    }

    [Fact]
    public void For_GivenManager_DoesNotHoldThePlatformPermission()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.Manager);

        // Assert
        permissions.Should().NotContain(Permission.PlatformAdminister);
    }

    [Fact]
    public void For_GivenUser_DoesNotHoldTheSupervisorPermissions()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.User);

        // Assert
        permissions.Should().NotContain(Permission.CasesAssign);
        permissions.Should().NotContain(Permission.CasesDelete);
        permissions.Should().NotContain(Permission.TransactionsReveal);
        permissions.Should().NotContain(Permission.SuspiciousActivityReportsFile);
        permissions.Should().NotContain(Permission.ApprovalsDecide);
        permissions.Should().NotContain(Permission.RulesWrite);
        permissions.Should().NotContain(Permission.RulesDeploy);
    }

    [Fact]
    public void For_GivenUser_HoldsTheDeskPermissions()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.User);

        // Assert
        permissions.Should().Contain(Permission.CasesRead);
        permissions.Should().Contain(Permission.CasesWrite);
        permissions.Should().Contain(Permission.CasesResolve);
        permissions.Should().Contain(Permission.CaseNotesWrite);
        permissions.Should().Contain(Permission.AttachmentsWrite);
        permissions.Should().Contain(Permission.DecisionsResolve);
        permissions.Should().Contain(Permission.QueuesClaim);
        permissions.Should().Contain(Permission.SuspiciousActivityReportsWrite);
    }

    [Fact]
    public void For_GivenAuditor_HoldsNoPermissionThatWrites()
    {
        // Arrange
        var writes = Enum.GetValues<Permission>()
            .Where(x => PermissionHelper.ToPermissionString(x).EndsWith(":write", StringComparison.Ordinal))
            .ToList();

        // Act
        var permissions = PermissionBundle.For(UserRole.Auditor);

        // Assert
        permissions.Should().NotIntersectWith(writes);
    }

    [Fact]
    public void For_GivenAuditor_HoldsNoPermissionThatChangesAnything()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.Auditor);

        // Assert
        permissions.Should().NotContain(Permission.TransactionsReveal);
        permissions.Should().NotContain(Permission.CasesAssign);
        permissions.Should().NotContain(Permission.CasesResolve);
        permissions.Should().NotContain(Permission.CasesDelete);
        permissions.Should().NotContain(Permission.DecisionsResolve);
        permissions.Should().NotContain(Permission.ApprovalsDecide);
        permissions.Should().NotContain(Permission.QueuesClaim);
        permissions.Should().NotContain(Permission.RulesDeploy);
        permissions.Should().NotContain(Permission.RulesEvaluate);
        permissions.Should().NotContain(Permission.RulesTest);
        permissions.Should().NotContain(Permission.RuleSetsEvaluate);
        permissions.Should().NotContain(Permission.SuspiciousActivityReportsFile);
        permissions.Should().NotContain(Permission.RolesAssign);
        permissions.Should().NotContain(Permission.PlatformAdminister);
    }

    [Fact]
    public void For_GivenAuditor_SeesEverythingTheSupervisorCanRead()
    {
        // Arrange
        var reads = PermissionBundle.For(UserRole.Manager)
            .Where(x => PermissionHelper.ToPermissionString(x).EndsWith(":read", StringComparison.Ordinal))
            .ToList();

        // Act
        var permissions = PermissionBundle.For(UserRole.Auditor);

        // Assert
        permissions.Should().Contain(reads);
    }

    [Fact]
    public void For_GivenAuditor_CanReadAndVerifyTheTrail()
    {
        // Act
        var permissions = PermissionBundle.For(UserRole.Auditor);

        // Assert
        permissions.Should().Contain(Permission.AuditLogsRead);
        permissions.Should().Contain(Permission.AuditLogsExport);
        permissions.Should().Contain(Permission.AuditLogsVerify);
        permissions.Should().Contain(Permission.CasesEvidencePack);
    }

    [Fact]
    public void For_GivenSeveralRoles_UnionsTheirBundles()
    {
        // Act
        var permissions = PermissionBundle.For([UserRole.User, UserRole.Auditor]);

        // Assert
        permissions.Should().Contain(PermissionBundle.For(UserRole.User));
        permissions.Should().Contain(PermissionBundle.For(UserRole.Auditor));
    }

    [Fact]
    public void IsGrantableToCustomRole_RefusesOnlyThePlatformPermission()
    {
        // Act
        var refused = Enum.GetValues<Permission>()
            .Where(x => !PermissionBundle.IsGrantableToCustomRole(x))
            .ToList();

        // Assert
        refused.Should().Equal(Permission.PlatformAdminister);
    }

    [Fact]
    public void For_GivenAnUndefinedRole_Throws()
    {
        // Act
        var act = () => PermissionBundle.For((UserRole)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
