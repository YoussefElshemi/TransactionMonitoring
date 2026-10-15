using AutoFixture;
using Presentation.Models.Plans;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakeDeletePlanRequestDto
{
    public static DeletePlanRequestDto CreateValid(IFixture fixture)
    {
        return new DeletePlanRequestDto
        {
            PlanId = fixture.Create<Guid>()
        };
    }
}
