using Core.Enums;
using Core.Helpers;
using Core.Models.Queues;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class QueueAssigneeResolverTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_ManualQueue_HandsOutNothing()
    {
        // Arrange
        var memberLoads = new List<QueueMemberLoad> { Member(openCaseCount: 0, lastAssignedAt: null) };

        // Act
        var assignee = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.Manual, memberLoads);

        // Assert
        assignee.Should().BeNull();
    }

    [Theory]
    [InlineData(QueueRoutingPolicy.RoundRobin)]
    [InlineData(QueueRoutingPolicy.LeastLoaded)]
    public void Resolve_QueueWithNobodyOnIt_LeavesTheCaseWaiting(QueueRoutingPolicy routingPolicy)
    {
        // Act
        var assignee = QueueAssigneeResolver.Resolve(routingPolicy, []);

        // Assert
        assignee.Should().BeNull();
    }

    [Fact]
    public void Resolve_RoundRobin_TakesWhoeverWaitedLongest()
    {
        // Arrange
        var stalest = Member(openCaseCount: 9, lastAssignedAt: Now.AddHours(-5));
        var recent = Member(openCaseCount: 0, lastAssignedAt: Now.AddMinutes(-1));

        // Act
        var assignee = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.RoundRobin, [recent, stalest]);

        // Assert
        assignee.Should().Be(stalest.UserId);
    }

    [Fact]
    public void Resolve_RoundRobin_GivesANewMemberTheNextCase()
    {
        // Arrange
        var newJoiner = Member(openCaseCount: 0, lastAssignedAt: null);
        var established = Member(openCaseCount: 0, lastAssignedAt: Now.AddDays(-7));

        // Act
        var assignee = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.RoundRobin, [established, newJoiner]);

        // Assert
        assignee.Should().Be(newJoiner.UserId);
    }

    [Fact]
    public void Resolve_LeastLoaded_TakesWhoeverIsCarryingLeast()
    {
        // Arrange
        var busy = Member(openCaseCount: 12, lastAssignedAt: Now.AddDays(-3));
        var free = Member(openCaseCount: 1, lastAssignedAt: Now.AddMinutes(-1));

        // Act
        var assignee = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.LeastLoaded, [busy, free]);

        // Assert
        assignee.Should().Be(free.UserId);
    }

    [Fact]
    public void Resolve_LeastLoaded_EqualLoads_FallsBackToTheRotation()
    {
        // Arrange
        var recentlyAssigned = Member(openCaseCount: 3, lastAssignedAt: Now.AddMinutes(-1));
        var waiting = Member(openCaseCount: 3, lastAssignedAt: Now.AddHours(-4));

        // Act
        var assignee = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.LeastLoaded, [recentlyAssigned, waiting]);

        // Assert
        assignee.Should().Be(waiting.UserId);
    }

    [Fact]
    public void Resolve_RoundRobin_TwoUntouchedMembers_IsStableRatherThanArbitrary()
    {
        // Arrange
        var first = Member(openCaseCount: 0, lastAssignedAt: null);
        var second = Member(openCaseCount: 0, lastAssignedAt: null);
        var expected = first.UserId < (Guid)second.UserId ? first.UserId : second.UserId;

        // Act
        var oneWay = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.RoundRobin, [first, second]);
        var theOther = QueueAssigneeResolver.Resolve(QueueRoutingPolicy.RoundRobin, [second, first]);

        // Assert
        oneWay.Should().Be(expected);
        theOther.Should().Be(expected);
    }

    private static QueueMemberLoad Member(int openCaseCount, DateTimeOffset? lastAssignedAt)
    {
        return new QueueMemberLoad
        {
            UserId = new ApplicationUserId(Guid.NewGuid()),
            User = null,
            OpenCaseCount = new OpenCaseCount(openCaseCount),
            LastAssignedAt = lastAssignedAt.HasValue ? new LastAssignedAt(lastAssignedAt.Value) : null
        };
    }
}
