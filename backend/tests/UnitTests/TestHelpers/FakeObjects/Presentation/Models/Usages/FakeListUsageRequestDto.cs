using AutoFixture;
using Presentation.Models.Usages;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Usages;

public static class FakeListUsageRequestDto
{
    public static ListUsageRequestDto CreateValid(IFixture fixture)
    {
        return new ListUsageRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>(),
            Resource = fixture.Create<string>(),
            PageNumber = fixture.Create<int>(),
            PageSize = fixture.Create<int>()
        };
    }
}