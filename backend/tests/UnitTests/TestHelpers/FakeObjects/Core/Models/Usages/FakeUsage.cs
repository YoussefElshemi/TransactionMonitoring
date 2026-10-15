using AutoFixture;
using Core.Enums;
using Core.Models.Usages;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Usages;

public static class FakeUsage
{
    public static Usage CreateValid(IFixture fixture)
    {
        return new Usage
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            UsageId = FakeUsageId.CreateValid(fixture),
            Resource = FakeResource.CreateValid(fixture),
            BillableResource = BillableResource.RuleEvaluation,
            Count = FakeCount.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture)
        };
    }
}