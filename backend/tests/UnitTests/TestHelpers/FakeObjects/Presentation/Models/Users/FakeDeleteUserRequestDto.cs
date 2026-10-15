using AutoFixture;
using Presentation.Models.Users;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

public static class FakeDeleteUserRequestDto
{
    public static DeleteUserRequestDto CreateValid(IFixture fixture)
    {
        return new DeleteUserRequestDto
        {
            UserId = fixture.Create<Guid>()
        };
    }
}