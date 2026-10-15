using AutoFixture;
using Core.Enums;
using Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeCreateApiClientRequest
{
    public static CreateApiClientRequest CreateValid(IFixture fixture)
    {
        return new CreateApiClientRequest
        {
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            UserId = FakeApplicationUserId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Scopes = [ApiScope.RulesEvaluate, ApiScope.RuleSetsEvaluate]
        };
    }
}
