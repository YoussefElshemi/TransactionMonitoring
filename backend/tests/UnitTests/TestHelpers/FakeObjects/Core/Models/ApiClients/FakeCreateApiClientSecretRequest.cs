using AutoFixture;
using Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeCreateApiClientSecretRequest
{
    public static CreateApiClientSecretRequest CreateValid(IFixture fixture)
    {
        return new CreateApiClientSecretRequest
        {
            ApiClientId = FakeApiClientId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            UserId = FakeApplicationUserId.CreateValid(fixture),
            ExpiresAt = null
        };
    }
}
