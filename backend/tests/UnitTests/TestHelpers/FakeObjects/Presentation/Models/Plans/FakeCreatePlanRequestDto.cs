using AutoFixture;
using Core.Enums;
using Presentation.Models.Plans;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakeCreatePlanRequestDto
{
    public static CreatePlanRequestDto CreateValid(IFixture fixture)
    {
        return new CreatePlanRequestDto
        {
            Body = new CreatePlanRequestBodyDto
            {
                Name = fixture.Create<string>(),
                Description = fixture.Create<string>(),
                OrganizationId = fixture.Create<Guid>(),
                CostPerRequest = 1,
                Currency = Currency.Gbp
            },
            CreatedByUserId = fixture.Create<Guid>()
        };
    }
}
