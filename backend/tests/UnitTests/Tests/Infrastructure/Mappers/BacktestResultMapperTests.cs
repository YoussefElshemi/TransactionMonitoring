using System.Text.Json.Nodes;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class BackTestResultMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_BackTestResultEntity()
    {
        // Arrange
        var backTestResultEntity = FakeBackTestResultEntity.CreateValid(Fixture);

        // Act
        var payload = new JsonObject();
        var backTestResult = backTestResultEntity.ToModel(payload);

        // Assert
        backTestResult.BackTestResultId.Should().Be(new BackTestResultId(backTestResultEntity.BackTestResultId));
        backTestResult.BackTestId.Should().Be(new BackTestId(backTestResultEntity.BackTestId));
        backTestResult.RuleEvaluationId.Should().Be(new RuleEvaluationId(backTestResultEntity.RuleEvaluationId));
        backTestResult.Payload.Should().BeSameAs(payload);
        backTestResult.OriginalMatched.Should().Be(new Matched(backTestResultEntity.OriginalMatched));
        backTestResult.NewMatched.Should().Be(new Matched(backTestResultEntity.NewMatched));
        backTestResult.HasChanged.Should().Be(new HasChanged(backTestResultEntity.HasChanged));
        backTestResult.CreatedAt.Should().Be(new CreatedAt(backTestResultEntity.CreatedAt));
        backTestResult.UpdatedAt.Should().Be(new UpdatedAt(backTestResultEntity.UpdatedAt));
    }

    [Fact]
    public void ToEntity_BackTestResult()
    {
        // Arrange
        var backTestResult = FakeBackTestResult.CreateValid(Fixture);

        // Act
        var backTestResultEntity = backTestResult.ToEntity();

        // Assert
        backTestResultEntity.BackTestResultId.Should().Be(backTestResult.BackTestResultId);
        backTestResultEntity.BackTestId.Should().Be(backTestResult.BackTestId);
        backTestResultEntity.RuleEvaluationId.Should().Be(backTestResult.RuleEvaluationId);
        backTestResultEntity.OriginalMatched.Should().Be(backTestResult.OriginalMatched);
        backTestResultEntity.NewMatched.Should().Be(backTestResult.NewMatched);
        backTestResultEntity.HasChanged.Should().Be(backTestResult.HasChanged);
        backTestResultEntity.CreatedAt.Should().Be(backTestResult.CreatedAt);
        backTestResultEntity.UpdatedAt.Should().Be(backTestResult.UpdatedAt);
    }
}
