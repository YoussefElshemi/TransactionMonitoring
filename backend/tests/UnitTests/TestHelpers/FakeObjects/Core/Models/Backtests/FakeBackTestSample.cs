using System.Text.Json.Nodes;
using AutoFixture;
using Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;

public static class FakeBackTestSample
{
    public static BackTestSample CreateValid(IFixture fixture)
    {
        return new BackTestSample
        {
            RuleEvaluationId = FakeRuleEvaluationId.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            Payload = new JsonObject { ["amount"] = fixture.Create<int>() },
            Matched = FakeMatched.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture)
        };
    }
}
