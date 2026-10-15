using AutoFixture;
using Core.Enums;
using Core.Extensions;
using Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeTransactionEntity
{
    public static TransactionEntity CreateValid(IFixture fixture)
    {
        return new TransactionEntity
        {
            ModeId = (int)Mode.Live,
            TransactionId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            FriendlyName = fixture.Create<string>(),
            ExternalReference = fixture.Create<string>(),
            Payload = FakeJsonObject.CreateValid(fixture).Serialize().ToBase64String(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
