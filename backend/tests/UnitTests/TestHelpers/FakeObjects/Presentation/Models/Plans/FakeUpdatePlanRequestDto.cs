using AutoFixture;
using Presentation.Models.Plans;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakeUpdatePlanRequestDto
{
    public static UpdatePlanRequestDto CreateValid(IFixture fixture)
    {
        return new UpdatePlanRequestDto
        {
            PlanId = fixture.Create<Guid>(),
            Body = new UpdatePlanRequestBodyDto
            {
                Name = fixture.Create<string>(),
                Description = fixture.Create<string>(),
                IsEnabled = true
            }
        };
    }
}
