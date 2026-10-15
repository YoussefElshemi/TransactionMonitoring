using AutoFixture;
using Presentation.Models.CaseNotes;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

public static class FakeUpdateCaseNoteRequestDto
{
    public static UpdateCaseNoteRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateCaseNoteRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            CaseNoteId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateCaseNoteRequestBodyDto
            {
                Message = fixture.Create<string>()
            }
        };
    }

    public static UpdateCaseNoteRequestDto CreateInvalid(IFixture fixture)
    {
        return new UpdateCaseNoteRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            CaseNoteId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateCaseNoteRequestBodyDto
            {
                Message = new string('A', 2049)
            }
        };
    }
}