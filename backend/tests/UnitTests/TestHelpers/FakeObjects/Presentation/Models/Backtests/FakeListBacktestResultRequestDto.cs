using AutoFixture;
using Presentation.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

public static class FakeListBackTestResultRequestDto
{
    public static ListBackTestResultRequestDto CreateValid(IFixture fixture)
    {
        return new ListBackTestResultRequestDto
        {
            BackTestId = fixture.Create<Guid>(),
            ChangedOnly = fixture.Create<bool>(),
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
