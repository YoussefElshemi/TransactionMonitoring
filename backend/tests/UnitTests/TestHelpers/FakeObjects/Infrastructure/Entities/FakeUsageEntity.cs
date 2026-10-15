using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeUsageEntity
{
    public static UsageEntity CreateValid(IFixture fixture)
    {
        return new UsageEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            UsageId = fixture.Create<Guid>(),
            Resource = fixture.Create<string>(),
            BillableResourceId = (int)BillableResource.RuleEvaluation,
            Count = fixture.Create<int>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture)
        };
    }
}