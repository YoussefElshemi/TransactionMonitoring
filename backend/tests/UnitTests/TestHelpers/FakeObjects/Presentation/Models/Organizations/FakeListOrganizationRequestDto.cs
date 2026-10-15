using AutoFixture;
using Presentation.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

public static class FakeListOrganizationRequestDto
{
    public static ListOrganizationRequestDto CreateValid(IFixture fixture)
    {
        return new ListOrganizationRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            FriendlyName = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}