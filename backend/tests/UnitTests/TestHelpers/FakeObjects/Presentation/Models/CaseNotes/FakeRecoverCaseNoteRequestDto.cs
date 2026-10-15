using AutoFixture;
using Presentation.Models.CaseNotes;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

public static class FakeRecoverCaseNoteRequestDto
{
    public static RecoverCaseNoteRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverCaseNoteRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            CaseNoteId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}