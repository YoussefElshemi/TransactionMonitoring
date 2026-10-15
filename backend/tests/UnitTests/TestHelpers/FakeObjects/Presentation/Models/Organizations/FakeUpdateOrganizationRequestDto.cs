using AutoFixture;
using Core.Enums;
using Presentation.Models.Organizations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

public static class FakeUpdateOrganizationRequestDto
{
    public static UpdateOrganizationRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateOrganizationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateOrganizationRequestBodyDto
            {
                FriendlyName = fixture.Create<string>(),
                Status = fixture.Create<OrganizationStatus>().ToString()
            }
        };
    }

    public static UpdateOrganizationRequestDto CreateInvalid(IFixture fixture)
    {
        return new UpdateOrganizationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateOrganizationRequestBodyDto
            {
                FriendlyName = new string('A', 51),
                Status = fixture.Create<OrganizationStatus>().ToString()
            }
        };
    }
}