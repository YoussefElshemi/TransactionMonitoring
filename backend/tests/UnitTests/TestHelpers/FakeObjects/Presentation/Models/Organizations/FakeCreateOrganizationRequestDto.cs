using AutoFixture;
using Presentation.Models.Organizations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

public static class FakeCreateOrganizationRequestDto
{
    public static CreateOrganizationRequestDto CreateValid(IFixture fixture)
    {
        return new CreateOrganizationRequestDto
        {
            FriendlyName = fixture.Create<string>(),
        };
    }

    public static CreateOrganizationRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateOrganizationRequestDto
        {
            FriendlyName = new string('A', 51)
        };
    }
}