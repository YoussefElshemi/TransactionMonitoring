using AutoFixture;
using Presentation.Models.CaseNotes;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

public static class FakeCaseNoteRequestDto
{
    public static CaseNoteRequestDto CreateValid(IFixture fixture)
    {
        return new CaseNoteRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            CaseNoteId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}