using AutoFixture;
using Core.Enums;
using Presentation.Models.Plans;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakePublishPlanVersionRequestDto
{
    public static PublishPlanVersionRequestDto CreateValid(IFixture fixture)
    {
        return new PublishPlanVersionRequestDto
        {
            PlanId = fixture.Create<Guid>(),
            Body = new PublishPlanVersionRequestBodyDto
            {
                CostPerRequest = 1,
                Currency = Currency.Gbp
            },
            PublishedByUserId = fixture.Create<Guid>()
        };
    }
}
