using Core.Enums;
using Core.Models.Permissions;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Models;

public class PermissionScopeTests : BaseTestClass
{
    private static readonly OrganizationId Organization = new(Guid.NewGuid());
    private static readonly SubOrganizationId Unit = new(Guid.NewGuid());
    private static readonly SubOrganizationId OtherUnit = new(Guid.NewGuid());
    private static readonly QueueId Queue = new(Guid.NewGuid());
    private static readonly QueueId OtherQueue = new(Guid.NewGuid());
    private static readonly RuleSetId RuleSet = new(Guid.NewGuid());
    private static readonly RuleSetId OtherRuleSet = new(Guid.NewGuid());

    [Fact]
    public void Covers_GivenAnOrganizationScope_CoversEverything()
    {
        // Arrange
        var scope = PermissionScope.Organization();

        // Act
        var covers = scope.Covers(CaseIn(Queue, Unit));

        // Assert
        covers.Should().BeTrue();
    }

    [Fact]
    public void Covers_GivenAnOrganizationScope_CoversTheUnrouted()
    {
        // Arrange
        var scope = PermissionScope.Organization();

        // Act
        var covers = scope.Covers(CaseIn(null, null));

        // Assert
        covers.Should().BeTrue();
    }

    [Fact]
    public void Covers_GivenAQueueScope_CoversThatQueueOnly()
    {
        // Arrange
        var scope = PermissionScope.ForQueue(Queue);

        // Act
        var covers = scope.Covers(CaseIn(Queue, Unit));
        var coversOther = scope.Covers(CaseIn(OtherQueue, Unit));

        // Assert
        covers.Should().BeTrue();
        coversOther.Should().BeFalse();
    }

    [Fact]
    public void Covers_GivenAQueueScope_DoesNotCoverTheUnrouted()
    {
        // Arrange
        var scope = PermissionScope.ForQueue(Queue);

        // Act
        var covers = scope.Covers(CaseIn(null, null));

        // Assert
        covers.Should().BeFalse();
    }

    [Fact]
    public void Covers_GivenASubOrganizationScope_CoversAQueueInThatUnit()
    {
        // Arrange
        var scope = PermissionScope.ForSubOrganization(Unit);

        // Act
        var covers = scope.Covers(CaseIn(Queue, Unit));
        var coversOther = scope.Covers(CaseIn(OtherQueue, OtherUnit));

        // Assert
        covers.Should().BeTrue();
        coversOther.Should().BeFalse();
    }

    [Fact]
    public void Covers_GivenARuleSetScope_CoversThatRuleSetOnly()
    {
        // Arrange
        var scope = PermissionScope.ForRuleSet(RuleSet);

        // Act
        var covers = scope.Covers(RuleIn(RuleSet, null));
        var coversOther = scope.Covers(RuleIn(OtherRuleSet, null));

        // Assert
        covers.Should().BeTrue();
        coversOther.Should().BeFalse();
    }

    [Fact]
    public void Covers_GivenASubOrganizationScope_CoversARuleSetInThatUnit()
    {
        // Arrange
        var scope = PermissionScope.ForSubOrganization(Unit);

        // Act
        var covers = scope.Covers(RuleIn(RuleSet, Unit));

        // Assert
        covers.Should().BeTrue();
    }

    [Fact]
    public void NarrowTo_GivenAnOrganizationScope_BecomesTheUnit()
    {
        // Act
        var narrowed = PermissionScope.Organization().NarrowTo(Unit);

        // Assert
        narrowed.ScopeType.Should().Be(PermissionScopeType.SubOrganization);
        narrowed.SubOrganizationId.Should().Be(Unit);
    }

    [Fact]
    public void NarrowTo_GivenAQueueScope_IsUnchanged()
    {
        // Arrange
        var scope = PermissionScope.ForQueue(Queue);

        // Act
        var narrowed = scope.NarrowTo(Unit);

        // Assert
        narrowed.Should().Be(scope);
    }

    [Fact]
    public void NarrowTo_GivenASubOrganizationScope_IsUnchanged()
    {
        // Arrange
        var scope = PermissionScope.ForSubOrganization(OtherUnit);

        // Act
        var narrowed = scope.NarrowTo(Unit);

        // Assert
        narrowed.Should().Be(scope);
    }

    private static ResourceScope CaseIn(QueueId? queueId, SubOrganizationId? subOrganizationId)
    {
        return new ResourceScope
        {
            OrganizationId = Organization,
            SubOrganizationId = subOrganizationId,
            RuleSetId = null,
            QueueId = queueId
        };
    }

    private static ResourceScope RuleIn(RuleSetId? ruleSetId, SubOrganizationId? subOrganizationId)
    {
        return new ResourceScope
        {
            OrganizationId = Organization,
            SubOrganizationId = subOrganizationId,
            RuleSetId = ruleSetId,
            QueueId = null
        };
    }
}
