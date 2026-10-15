using AutoFixture;
using Presentation.Models.Cases;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeUnassignCaseRequestDto
{
    public static UnassignCaseRequestDto CreateValid(IFixture fixture)
    {
        return new UnassignCaseRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}