using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Mappers;

public class CreateBackTestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenCreateBackTestRequest_MapsCorrectly()
    {
        // Arrange
        var createBackTestRequest = FakeCreateBackTestRequest.CreateValid(Fixture);
        var ruleVersionId = FakeRuleVersionId.CreateValid(Fixture);

        // Act
        var backTest = createBackTestRequest.ToModel(ruleVersionId, MockTimeProvider, Mode.Live);

        // Assert
        backTest.BackTestId.Should().NotBeNull();
        backTest.OrganizationId.Should().Be(createBackTestRequest.OrganizationId);
        backTest.RuleId.Should().Be(createBackTestRequest.RuleId);
        backTest.RuleVersionId.Should().Be(ruleVersionId);
        backTest.CreatedByUserId.Should().Be(createBackTestRequest.CreatedByUserId);
        backTest.SampleSize.Should().Be(createBackTestRequest.SampleSize);
        backTest.Status.Should().Be(BackTestStatus.Pending);
        backTest.TotalEvaluations.Should().Be(new TotalCount(0));
        backTest.ProcessedCount.Should().Be(new ProcessedCount(0));
        backTest.CompletedAt.Should().BeNull();
    }
}
