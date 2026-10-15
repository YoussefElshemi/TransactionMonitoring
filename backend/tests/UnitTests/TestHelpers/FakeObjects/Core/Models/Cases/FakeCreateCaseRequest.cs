using System.Text.Json.Nodes;
using AutoFixture;
using Core.Enums;
using Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

public static class FakeCreateCaseRequest
{
    public static CreateCaseRequest CreateValid(IFixture fixture)
    {
        return new CreateCaseRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Severity = fixture.Create<RuleSeverity>(),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            PartyId = FakePartyId.CreateValid(fixture),
            RuleEvaluationIds = [],
            RuleIds = [],
            Payload = new JsonObject(),
            Parties = [],
            HoldPolicy = null
        };
    }
}