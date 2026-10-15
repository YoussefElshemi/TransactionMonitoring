using AutoFixture;
using Presentation.Models.CaseNotes;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

public static class FakeCreateCaseNoteRequestDto
{
    public static CreateCaseNoteRequestDto CreateValid(IFixture fixture)
    {
        return new CreateCaseNoteRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateCaseNoteRequestBodyDto
            {
                Message = fixture.Create<string>()
            }
        };
    }

    public static CreateCaseNoteRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateCaseNoteRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateCaseNoteRequestBodyDto
            {
                Message = new string('A', 2049)
            }
        };
    }
}