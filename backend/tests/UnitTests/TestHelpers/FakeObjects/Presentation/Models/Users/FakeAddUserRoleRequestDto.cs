using AutoFixture;
using Core.Enums;
using Presentation.Models.Users;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

public static class FakeAddUserRoleRequestDto
{
    public static AddUserRoleRequestDto CreateValid(IFixture fixture)
    {
        return new AddUserRoleRequestDto
        {
            UserId = fixture.Create<Guid>(),
            Role = fixture.Create<UserRole>().ToString()
        };
    }
}