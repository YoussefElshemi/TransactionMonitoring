using AutoFixture;
using Core.Enums;
using Core.Helpers;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeApiClientSecretEntity
{
    public static ApiClientSecretEntity CreateValid(IFixture fixture)
    {
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);

        return new ApiClientSecretEntity
        {
            ModeId = (int)Mode.Live,
            ApiClientSecretId = fixture.Create<Guid>(),
            ApiClientId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            SecretHash = ApiClientCredentialHelper.Hash(clientSecret),
            SecretPrefix = ApiClientCredentialHelper.ToSecretPrefix(clientSecret),
            ExpiresAt = null,
            RevokedAt = null,
            LastUsedAt = null,
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = false,
            DeletedAt = null
        };
    }
}
