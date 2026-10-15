using AutoFixture;
using Core.Constants;
using Core.Enums;
using Core.Models.ApiClients;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeApiClient
{
    public static ApiClient CreateValid(IFixture fixture)
    {
        return new ApiClient
        {
            Mode = Mode.Live,
            ApiVersion = ApiVersions.Current,
            ApiClientId = FakeApiClientId.CreateValid(fixture),
            ClientId = FakeClientId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            IsEnabled = new IsEnabled(true),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Organization = FakeOrganization.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            Scopes = [ApiScope.RulesEvaluate],
            Secrets = [FakeApiClientSecret.CreateValid(fixture)],
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
