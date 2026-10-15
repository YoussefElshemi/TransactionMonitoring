using AutoFixture;
using Core.Enums;
using Core.Models.Usages;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Usages;

public static class FakeCreateUsageRequest
{
    public static CreateUsageRequest CreateValid(IFixture fixture)
    {
        return new CreateUsageRequest
        {
            Resource = FakeResource.CreateValid(fixture),
            BillableResource = BillableResource.RuleEvaluation,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Count = FakeCount.CreateValid(fixture)
        };
    }
}