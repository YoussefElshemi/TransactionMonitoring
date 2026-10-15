using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class BackTestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_BackTestEntity()
    {
        // Arrange
        var backTestEntity = FakeBackTestEntity.CreateValid(Fixture);

        // Act
        var backTest = backTestEntity.ToModel();

        // Assert
        backTest.BackTestId.Should().Be(new BackTestId(backTestEntity.BackTestId));
        backTest.OrganizationId.Should().Be(new OrganizationId(backTestEntity.OrganizationId));
        backTest.RuleId.Should().Be(new RuleId(backTestEntity.RuleId));
        backTest.RuleVersionId.Should().Be(new RuleVersionId(backTestEntity.RuleVersionId));
        backTest.Status.Should().Be((BackTestStatus)backTestEntity.StatusId);
        backTest.SampleSize.Should().Be(new SampleSize(backTestEntity.SampleSize));
        backTest.TotalEvaluations.Should().Be(new TotalCount(backTestEntity.TotalEvaluations));
        backTest.ProcessedCount.Should().Be(new ProcessedCount(backTestEntity.ProcessedCount));
        backTest.CreatedByUserId.Should().Be(new ApplicationUserId(backTestEntity.CreatedByUserId));
        backTest.CompletedAt.Should().Be(new CompletedAt(backTestEntity.CompletedAt!.Value));
        backTest.CreatedAt.Should().Be(new CreatedAt(backTestEntity.CreatedAt));
        backTest.UpdatedAt.Should().Be(new UpdatedAt(backTestEntity.UpdatedAt));
    }

    [Fact]
    public void ToEntity_BackTest()
    {
        // Arrange
        var backTest = FakeBackTest.CreateValid(Fixture);

        // Act
        var backTestEntity = backTest.ToEntity();

        // Assert
        backTestEntity.BackTestId.Should().Be(backTest.BackTestId);
        backTestEntity.OrganizationId.Should().Be(backTest.OrganizationId);
        backTestEntity.RuleId.Should().Be(backTest.RuleId);
        backTestEntity.RuleVersionId.Should().Be(backTest.RuleVersionId);
        backTestEntity.StatusId.Should().Be((int)backTest.Status);
        backTestEntity.SampleSize.Should().Be(backTest.SampleSize);
        backTestEntity.TotalEvaluations.Should().Be(backTest.TotalEvaluations);
        backTestEntity.ProcessedCount.Should().Be(backTest.ProcessedCount);
        backTestEntity.CreatedByUserId.Should().Be(backTest.CreatedByUserId);
        backTestEntity.CompletedAt.Should().Be(backTest.CompletedAt!.Value);
        backTestEntity.CreatedAt.Should().Be(backTest.CreatedAt);
        backTestEntity.UpdatedAt.Should().Be(backTest.UpdatedAt);
    }
}
