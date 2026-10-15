using AutoFixture;
using Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

public static class FakeEvaluateRuleSetsRequest
{
    public static EvaluateRuleSetsRequest CreateValid(IFixture fixture)
    {
        return new EvaluateRuleSetsRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Payload = FakeJsonObject.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            CreateCase = FakeCreateCase.CreateValid(fixture)
        };
    }
}
