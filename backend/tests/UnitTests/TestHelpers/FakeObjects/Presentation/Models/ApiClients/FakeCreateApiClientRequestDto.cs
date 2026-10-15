using AutoFixture;
using Core.Enums;
using Core.Helpers;
using Presentation.Models.ApiClients;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeCreateApiClientRequestDto
{
    public static CreateApiClientRequestDto CreateValid(IFixture fixture)
    {
        return new CreateApiClientRequestDto
        {
            FriendlyName = fixture.Create<string>()[..20],
            Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate), ApiScopeHelper.ToScopeString(ApiScope.RuleSetsEvaluate)]
        };
    }

    public static CreateApiClientRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateApiClientRequestDto
        {
            FriendlyName = string.Empty,
            Scopes = [fixture.Create<string>()]
        };
    }
}
