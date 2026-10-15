using AutoFixture;
using Presentation.Models.Organizations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

public static class FakeOrganizationRequestDto
{
    public static OrganizationRequestDto CreateValid(IFixture fixture)
    {
        return new OrganizationRequestDto
        {
            OrganizationId = fixture.Create<Guid>()
        };
    }
}