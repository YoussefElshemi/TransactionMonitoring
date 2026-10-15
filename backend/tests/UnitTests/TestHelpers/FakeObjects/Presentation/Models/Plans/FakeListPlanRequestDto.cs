using AutoFixture;
using Presentation.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

public static class FakeListPlanRequestDto
{
    public static ListPlanRequestDto CreateValid(IFixture fixture)
    {
        return new ListPlanRequestDto
        {
            Name = fixture.Create<string>(),
            OwnerOrganizationId = fixture.Create<Guid>(),
            IncludeDisabled = false,
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
