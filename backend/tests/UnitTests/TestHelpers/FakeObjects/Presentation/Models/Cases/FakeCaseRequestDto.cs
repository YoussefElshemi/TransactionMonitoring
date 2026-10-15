using AutoFixture;
using Presentation.Models.Cases;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeCaseRequestDto
{
    public static CaseRequestDto CreateValid(IFixture fixture)
    {
        return new CaseRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            CaseId = fixture.Create<Guid>()
        };
    }
}