using AutoFixture;
using Presentation.Models.OrganizationUsers;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

public static class FakeOrganizationUserRequestDto
{
    public static OrganizationUserRequestDto CreateValid(IFixture fixture)
    {
        return new OrganizationUserRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            UserId = fixture.Create<Guid>()
        };
    }
}