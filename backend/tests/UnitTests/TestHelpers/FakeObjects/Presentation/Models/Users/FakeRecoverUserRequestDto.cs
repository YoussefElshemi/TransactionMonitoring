using AutoFixture;
using Presentation.Models.Users;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

public static class FakeRecoverUserRequestDto
{
    public static RecoverUserRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverUserRequestDto
        {
            UserId = fixture.Create<Guid>()
        };
    }
}