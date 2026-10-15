using AutoFixture;
using Core.Enums;
using Core.Models.ApiClients;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeApiClientSecret
{
    public static ApiClientSecret CreateValid(IFixture fixture)
    {
        return new ApiClientSecret
        {
            Mode = Mode.Live,
            ApiClientSecretId = FakeApiClientSecretId.CreateValid(fixture),
            ApiClientId = FakeApiClientId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            SecretHash = FakeSecretHash.CreateValid(fixture),
            SecretPrefix = FakeSecretPrefix.CreateValid(fixture),
            ExpiresAt = null,
            RevokedAt = null,
            LastUsedAt = null,
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
