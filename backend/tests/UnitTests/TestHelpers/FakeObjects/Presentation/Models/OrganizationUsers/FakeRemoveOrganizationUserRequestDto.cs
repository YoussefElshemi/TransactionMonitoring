using AutoFixture;
using Presentation.Models.OrganizationUsers;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

public static class FakeRemoveOrganizationUserRequestDto
{
    public static RemoveOrganizationUserRequestDto CreateValid(IFixture fixture)
    {
        return new RemoveOrganizationUserRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            UserId = fixture.Create<Guid>()
        };
    }
}