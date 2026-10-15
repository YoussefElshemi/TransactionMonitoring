using AutoFixture;
using Presentation.Models.OrganizationUsers;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

public static class FakeListOrganizationUserRequestDto
{
    public static ListOrganizationUserRequestDto CreateValid(IFixture fixture)
    {
        return new ListOrganizationUserRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            UserName = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}