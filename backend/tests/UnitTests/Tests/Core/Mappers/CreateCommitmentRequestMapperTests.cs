using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Commitments;

namespace UnitTests.Tests.Core.Mappers;

public class CreateCommitmentRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateCommitmentRequest()
    {
        // Arrange
        var request = FakeCreateCommitmentRequest.CreateValid(Fixture);
        var now = MockTimeProvider.GetUtcNow();

        // Act
        var commitment = request.ToModel(MockTimeProvider);

        // Assert
        commitment.CommitmentId.Should().NotBe(new CommitmentId(Guid.Empty));
        commitment.OrganizationId.Should().Be(request.OrganizationId);
        commitment.Amount.Should().Be(request.Amount);
        commitment.EffectiveFrom.Should().Be(request.EffectiveFrom!.Value);
        commitment.EffectiveTo.Should().Be(request.EffectiveTo);
        commitment.CreatedByUserId.Should().Be(request.CreatedByUserId);
        commitment.CreatedAt.Should().Be(new CreatedAt(now));
        commitment.UpdatedAt.Should().Be(new UpdatedAt(now));
        commitment.IsDeleted.Should().Be(new IsDeleted(false));
        commitment.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToModel_StartsTheTermWithNothingDrawn()
    {
        // Arrange
        var request = FakeCreateCommitmentRequest.CreateValid(Fixture);

        // Act
        var commitment = request.ToModel(MockTimeProvider);

        // Assert
        commitment.Drawn.Should().Be(new CommitmentAmount(0));
    }

    [Fact]
    public void ToModel_GivenNoStartDate_StartsItNow()
    {
        // Arrange
        var request = FakeCreateCommitmentRequest.CreateValid(Fixture) with { EffectiveFrom = null };

        // Act
        var commitment = request.ToModel(MockTimeProvider);

        // Assert
        commitment.EffectiveFrom.Should().Be(new EffectiveFrom(MockTimeProvider.GetUtcNow()));
    }

    [Fact]
    public void ToModel_GivesEachTermItsOwnIdentity()
    {
        // Arrange
        var request = FakeCreateCommitmentRequest.CreateValid(Fixture);

        // Act
        var first = request.ToModel(MockTimeProvider);
        var second = request.ToModel(MockTimeProvider);

        // Assert
        first.CommitmentId.Should().NotBe(second.CommitmentId);
    }
}
