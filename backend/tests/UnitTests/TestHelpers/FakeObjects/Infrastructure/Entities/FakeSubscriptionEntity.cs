using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeSubscriptionEntity
{
    public static SubscriptionEntity CreateValid(IFixture fixture)
    {
        return new SubscriptionEntity
        {
            SubscriptionId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            PlanVersionId = fixture.Create<Guid>(),
            PlanVersion = FakePlanVersionEntity.CreateValid(fixture),
            EffectiveFrom = fixture.Create<DateTimeOffset>(),
            EffectiveTo = fixture.Create<DateTimeOffset>(),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
