using System.Text.Json.Nodes;
using AutoFixture;
using Core.Models.BackTests;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;

public static class FakeBackTestResult
{
    public static BackTestResult CreateValid(IFixture fixture)
    {
        return new BackTestResult
        {
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            BackTestResultId = FakeBackTestResultId.CreateValid(fixture),
            BackTestId = FakeBackTestId.CreateValid(fixture),
            RuleEvaluationId = FakeRuleEvaluationId.CreateValid(fixture),
            Payload = new JsonObject(),
            OriginalMatched = new Matched(fixture.Create<bool>()),
            NewMatched = new Matched(fixture.Create<bool>()),
            HasChanged = new HasChanged(fixture.Create<bool>())
        };
    }
}
