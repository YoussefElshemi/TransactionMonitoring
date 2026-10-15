using AutoFixture;
using Presentation.Models.Organizations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

public static class FakeRecoverOrganizationRequestDto
{
    public static RecoverOrganizationRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverOrganizationRequestDto
        {
            OrganizationId = fixture.Create<Guid>()
        };
    }
}