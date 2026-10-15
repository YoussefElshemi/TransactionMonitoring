using FluentAssertions;
using Presentation.Mappers.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Mappers.CaseNotes;

public class CaseNoteDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_CaseNote()
    {
        // Arrange
        var caseNote = FakeCaseNote.CreateValid(Fixture);

        // Act
        var caseNoteDto = caseNote.ToDto();

        caseNoteDto.CaseNoteId.Should().Be(caseNote.CaseNoteId);
        caseNoteDto.CaseId.Should().Be(caseNote.CaseId);
        caseNoteDto.Message.Should().Be(caseNote.Message);
        caseNoteDto.CreatedAt.Should().Be(caseNote.CreatedAt);
        caseNoteDto.UpdatedAt.Should().Be(caseNote.UpdatedAt);
    }
}