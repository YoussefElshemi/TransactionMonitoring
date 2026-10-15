using AutoFixture;
using Core.Enums;
using Core.Helpers;
using Presentation.Models.ApiClients;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeUpdateApiClientRequestDto
{
    public static UpdateApiClientRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateApiClientRequestDto
        {
            ApiClientId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateApiClientRequestBodyDto
            {
                FriendlyName = fixture.Create<string>()[..20],
                IsEnabled = true,
                Scopes = [ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate)]
            }
        };
    }

    public static UpdateApiClientRequestDto CreateInvalid(IFixture fixture)
    {
        return new UpdateApiClientRequestDto
        {
            ApiClientId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateApiClientRequestBodyDto
            {
                FriendlyName = string.Empty,
                Scopes = [fixture.Create<string>()]
            }
        };
    }
}
