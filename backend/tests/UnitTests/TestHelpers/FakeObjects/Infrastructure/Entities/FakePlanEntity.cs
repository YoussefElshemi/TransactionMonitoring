using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakePlanEntity
{
    public static PlanEntity CreateValid(IFixture fixture)
    {
        return new PlanEntity
        {
            PlanId = fixture.Create<Guid>(),
            Name = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>(),
            IsEnabled = fixture.Create<bool>(),
            CurrentVersionNumber = fixture.Create<int>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
