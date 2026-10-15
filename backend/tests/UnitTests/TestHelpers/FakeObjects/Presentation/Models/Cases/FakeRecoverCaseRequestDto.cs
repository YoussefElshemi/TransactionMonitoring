using AutoFixture;
using Presentation.Models.Cases;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeRecoverCaseRequestDto
{
    public static RecoverCaseRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverCaseRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}