using AutoFixture;
using Presentation.Models.Users;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

public static class FakeUserRequestDto
{
    public static UserRequestDto CreateValid(IFixture fixture)
    {
        return new UserRequestDto
        {
            UserId = fixture.Create<Guid>()
        };
    }
}