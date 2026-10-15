using AutoFixture;
using Core.Enums;
using Presentation.Models.Users;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

public static class FakeRemoveUserRoleRequestDto
{
    public static RemoveUserRoleRequestDto CreateValid(IFixture fixture)
    {
        return new RemoveUserRoleRequestDto
        {
            UserId = fixture.Create<Guid>(),
            Role = fixture.Create<UserRole>().ToString()
        };
    }
}