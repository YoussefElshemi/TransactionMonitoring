using AutoFixture;
using Presentation.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

public static class FakeListBackTestRequestDto
{
    public static ListBackTestRequestDto CreateValid(IFixture fixture)
    {
        return new ListBackTestRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
