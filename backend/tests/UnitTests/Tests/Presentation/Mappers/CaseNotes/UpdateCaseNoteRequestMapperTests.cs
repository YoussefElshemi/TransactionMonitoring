using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Mappers.CaseNotes;

public class UpdateCaseNoteRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CaseNote()
    {
        // Arrange
        var updateCaseNoteRequestDto = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture);

        // Act
        var updateCaseNoteRequest = updateCaseNoteRequestDto.ToModel();

        // Assert
        updateCaseNoteRequest.CaseNoteId.Should().Be(new CaseNoteId(updateCaseNoteRequestDto.CaseNoteId));
        updateCaseNoteRequest.Message.Should().Be(new Message(updateCaseNoteRequestDto.Body.Message!));
    }
}