using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Commitments;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class CommitmentMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Commitment()
    {
        // Arrange
        var commitmentEntity = FakeCommitmentEntity.CreateValid(Fixture);

        // Act
        var commitment = commitmentEntity.ToModel(3000m);

        // Assert
        commitment.CommitmentId.Should().Be(new CommitmentId(commitmentEntity.CommitmentId));
        commitment.OrganizationId.Should().Be(new OrganizationId(commitmentEntity.OrganizationId));
        commitment.Amount.Should().Be(new CommitmentAmount(commitmentEntity.Amount));
        commitment.EffectiveFrom.Should().Be(new EffectiveFrom(commitmentEntity.EffectiveFrom));
        commitment.EffectiveTo.Should().Be(new EffectiveTo(commitmentEntity.EffectiveTo));
        commitment.CreatedByUserId.Should().Be(new ApplicationUserId(commitmentEntity.CreatedByUserId!.Value));
        commitment.CreatedAt.Should().Be(new CreatedAt(commitmentEntity.CreatedAt));
        commitment.UpdatedAt.Should().Be(new UpdatedAt(commitmentEntity.UpdatedAt));
        commitment.IsDeleted.Should().Be(new IsDeleted(commitmentEntity.IsDeleted));
        commitment.DeletedAt.Should().Be(new DeletedAt(commitmentEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToModel_TakesDrawnFromTheCallerRatherThanTheRow()
    {
        // Arrange
        var commitmentEntity = FakeCommitmentEntity.CreateValid(Fixture);

        // Act
        var commitment = commitmentEntity.ToModel(4750.25m);

        // Assert
        commitment.Drawn.Should().Be(new CommitmentAmount(4750.25m));
    }

    [Fact]
    public void ToEntity_Commitment()
    {
        // Arrange
        var commitment = FakeCommitment.CreateValid(Fixture);

        // Act
        var commitmentEntity = commitment.ToEntity();

        // Assert
        commitmentEntity.CommitmentId.Should().Be(commitment.CommitmentId);
        commitmentEntity.OrganizationId.Should().Be(commitment.OrganizationId);
        commitmentEntity.Amount.Should().Be(commitment.Amount);
        commitmentEntity.EffectiveFrom.Should().Be(commitment.EffectiveFrom);
        commitmentEntity.EffectiveTo.Should().Be(commitment.EffectiveTo);
        commitmentEntity.CreatedByUserId.Should().Be(commitment.CreatedByUserId!.Value);
        commitmentEntity.CreatedAt.Should().Be(commitment.CreatedAt);
        commitmentEntity.UpdatedAt.Should().Be(commitment.UpdatedAt);
        commitmentEntity.IsDeleted.Should().Be(commitment.IsDeleted);
        commitmentEntity.DeletedAt.Should().BeNull();
    }
}
