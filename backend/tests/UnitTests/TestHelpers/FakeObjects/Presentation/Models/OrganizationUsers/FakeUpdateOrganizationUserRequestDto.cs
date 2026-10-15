using AutoFixture;
using Presentation.Models.OrganizationUsers;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

public static class FakeUpdateOrganizationUserRequestDto
{
    public static UpdateOrganizationUserRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateOrganizationUserRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            UserId = fixture.Create<Guid>()
        };
    }
}