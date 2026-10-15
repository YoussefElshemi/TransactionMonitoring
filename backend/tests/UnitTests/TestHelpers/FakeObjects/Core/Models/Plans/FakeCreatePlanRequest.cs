using AutoFixture;
using Core.Enums;
using Core.Models.Plans;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;

public static class FakeCreatePlanRequest
{
    public static CreatePlanRequest CreateValid(IFixture fixture)
    {
        return new CreatePlanRequest
        {
            Name = FakePlanName.CreateValid(fixture),
            Description = FakePlanDescription.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            CostPerRequest = FakeCostPerRequest.CreateValid(fixture),
            Currency = Currency.Gbp,
            PlatformFee = new PlatformFee(0),
            MinimumCharge = new MinimumCharge(0),
            Prices = [],
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
