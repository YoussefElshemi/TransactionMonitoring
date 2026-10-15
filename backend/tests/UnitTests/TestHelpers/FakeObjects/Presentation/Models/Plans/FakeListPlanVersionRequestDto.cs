using AutoFixture;
using Presentation.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakeListPlanVersionRequestDto
{
    public static ListPlanVersionRequestDto CreateValid(IFixture fixture)
    {
        return new ListPlanVersionRequestDto
        {
            PlanId = fixture.Create<Guid>(),
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
