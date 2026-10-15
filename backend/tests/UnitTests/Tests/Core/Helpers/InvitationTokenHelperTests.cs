using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invitations;

namespace UnitTests.Tests.Core.Helpers;

public class InvitationTokenHelperTests : BaseTestClass
{
    [Fact]
    public void Generate_Always_ReturnsADifferentTokenEachTime()
    {
        // Arrange
        // Act
        var tokens = Enumerable.Range(0, 100).Select(_ => (string)InvitationTokenHelper.Generate()).ToList();

        // Assert
        tokens.Distinct().Should().HaveCount(100);
    }

    [Fact]
    public void Hash_TheSameToken_ProducesTheSameDigest()
    {
        // Arrange
        var token = InvitationTokenHelper.Generate();

        // Act
        var first = InvitationTokenHelper.Hash(token);
        var second = InvitationTokenHelper.Hash(token);

        // Assert
        ((string)first).Should().Be(second);
        ((string)first).Should().NotBe(token);
        ((string)first).Should().HaveLength(Hash.Length);
    }
}

public class InvitationStatusResolverTests : BaseTestClass
{
    [Fact]
    public void Resolve_NothingHasHappenedAndTheWindowIsOpen_IsPending()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture);

        // Act
        var status = InvitationStatusResolver.Resolve(invitation, TestTimeConstants.Today);

        // Assert
        status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void Resolve_TheWindowHasClosed_IsExpired()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture);

        // Act
        var status = InvitationStatusResolver.Resolve(invitation, TestTimeConstants.Today.AddDays(15));

        // Assert
        status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public void Resolve_TakenUpBeforeItLapsed_StaysAcceptedAfterwards()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture) with
        {
            AcceptedAt = new AcceptedAt(TestTimeConstants.Today.AddDays(1))
        };

        // Act
        var status = InvitationStatusResolver.Resolve(invitation, TestTimeConstants.Today.AddDays(400));

        // Assert
        status.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public void Resolve_WithdrawnAfterItHadAlreadyLapsed_ReadsAsRevoked()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture) with
        {
            ExpiresAt = new ExpiresAt(TestTimeConstants.Today.AddDays(-1)),
            RevokedAt = new RevokedAt(TestTimeConstants.Today)
        };

        // Act
        var status = InvitationStatusResolver.Resolve(invitation, TestTimeConstants.Today);

        // Assert
        status.Should().Be(InvitationStatus.Revoked);
    }
}
