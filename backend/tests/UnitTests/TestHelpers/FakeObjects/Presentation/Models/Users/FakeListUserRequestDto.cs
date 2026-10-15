using AutoFixture;
using Presentation.Models.Users;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

public static class FakeListUserRequestDto
{
    public static ListUserRequestDto CreateValid(IFixture fixture)
    {
        return new ListUserRequestDto
        {
            UserName = fixture.Create<string>(),
            PageNumber = fixture.Create<int>(),
            PageSize = fixture.Create<int>(),
            OrganizationId = fixture.Create<Guid>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>()
        };
    }
}