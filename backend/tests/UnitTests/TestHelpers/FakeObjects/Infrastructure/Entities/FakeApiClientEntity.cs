using AutoFixture;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeApiClientEntity
{
    public static ApiClientEntity CreateValid(IFixture fixture)
    {
        var apiClientId = fixture.Create<Guid>();

        return new ApiClientEntity
        {
            ModeId = (int)Mode.Live,
            ApiClientId = apiClientId,
            ClientId = ApiClientCredentialHelper.GenerateClientId(Mode.Live),
            FriendlyName = fixture.Create<string>(),
            IsEnabled = true,
            ApiVersion = ApiVersions.V20261014,
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture),
            Scopes =
            [
                new ApiClientScopeEntity
                {
                    ApiClientId = apiClientId,
                    ApiScopeId = (int)ApiScope.RulesEvaluate,
                    CreatedAt = fixture.Create<DateTimeOffset>(),
                    UpdatedAt = fixture.Create<DateTimeOffset>()
                }
            ],
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = false,
            DeletedAt = null
        };
    }
}
