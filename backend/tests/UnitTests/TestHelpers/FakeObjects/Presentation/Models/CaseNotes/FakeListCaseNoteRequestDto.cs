using AutoFixture;
using Presentation.Models.CaseNotes;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

public static class FakeListCaseNoteRequestDto
{
    public static ListCaseNoteRequestDto CreateValid(IFixture fixture)
    {
        return new ListCaseNoteRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            CaseId = fixture.Create<Guid>(),
            Message = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}