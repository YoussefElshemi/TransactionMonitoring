using Core.Enums;
using Core.Extensions;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Extensions;

public class CaseStatusExtensionsTests : BaseTestClass
{
    [Theory]
    [InlineData(CaseStatus.Approved)]
    [InlineData(CaseStatus.Rejected)]
    [InlineData(CaseStatus.Expired)]
    public void IsTerminal_GivenAClosedStatus_IsTrue(CaseStatus caseStatus)
    {
        // Arrange
        var isTerminal = caseStatus.IsTerminal();

        // Assert
        isTerminal.Should().BeTrue();
    }

    [Theory]
    [InlineData(CaseStatus.Pending)]
    [InlineData(CaseStatus.Assigned)]
    [InlineData(CaseStatus.InReview)]
    [InlineData(CaseStatus.Escalated)]
    [InlineData(CaseStatus.PendingInformation)]
    [InlineData(CaseStatus.OnHold)]
    public void IsTerminal_GivenADeskStatus_IsFalse(CaseStatus caseStatus)
    {
        // Arrange & Act
        var isTerminal = caseStatus.IsTerminal();

        // Assert
        isTerminal.Should().BeFalse();
    }

    [Fact]
    public void TerminalStatusIds_MatchesTheTerminalStatuses()
    {
        // Arrange & Act
        var terminalStatusIds = CaseStatusExtensions.TerminalStatusIds;

        // Assert
        terminalStatusIds.Should().BeEquivalentTo([(int)CaseStatus.Approved, (int)CaseStatus.Rejected, (int)CaseStatus.Expired]);
    }

    [Theory]
    [InlineData(CaseStatus.Assigned, CaseStatus.InReview)]
    [InlineData(CaseStatus.Assigned, CaseStatus.Escalated)]
    [InlineData(CaseStatus.Assigned, CaseStatus.PendingInformation)]
    [InlineData(CaseStatus.Assigned, CaseStatus.OnHold)]
    [InlineData(CaseStatus.InReview, CaseStatus.Escalated)]
    [InlineData(CaseStatus.Escalated, CaseStatus.InReview)]
    [InlineData(CaseStatus.PendingInformation, CaseStatus.OnHold)]
    [InlineData(CaseStatus.OnHold, CaseStatus.Escalated)]
    public void CanTransitionTo_GivenAnAllowedMove_IsTrue(CaseStatus from, CaseStatus to)
    {
        // Arrange
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Theory]
    [InlineData(CaseStatus.Pending, CaseStatus.InReview)]
    [InlineData(CaseStatus.InReview, CaseStatus.Approved)]
    [InlineData(CaseStatus.InReview, CaseStatus.Rejected)]
    [InlineData(CaseStatus.InReview, CaseStatus.Assigned)]
    [InlineData(CaseStatus.OnHold, CaseStatus.Pending)]
    [InlineData(CaseStatus.Approved, CaseStatus.InReview)]
    [InlineData(CaseStatus.Expired, CaseStatus.OnHold)]
    [InlineData(CaseStatus.InReview, CaseStatus.InReview)]
    public void CanTransitionTo_GivenAMoveWithItsOwnEndpoint_IsFalse(CaseStatus from, CaseStatus to)
    {
        // Arrange & Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeFalse();
    }
}
