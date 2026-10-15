using AutoFixture;
using Presentation.Models.Plans;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakePlanRequestDto
{
    public static PlanRequestDto CreateValid(IFixture fixture)
    {
        return new PlanRequestDto
        {
            PlanId = fixture.Create<Guid>()
        };
    }
}
