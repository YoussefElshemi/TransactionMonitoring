using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Mappers.CaseNotes;

public class ListCaseNoteRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListCaseNoteRequest()
    {
        // Arrange
        var listCaseNoteRequestDto = FakeListCaseNoteRequestDto.CreateValid(Fixture);

        // Act
        var listCaseNoteRequest = listCaseNoteRequestDto.ToModel();

        // Assert
        listCaseNoteRequest.CaseId.Should().Be(new CaseId(listCaseNoteRequestDto.CaseId));
        listCaseNoteRequest.Message.Should().Be(new Message(listCaseNoteRequestDto.Message!));
        listCaseNoteRequest.PageNumber.Should().Be(new PageNumber(listCaseNoteRequestDto.PageNumber));
        listCaseNoteRequest.PageSize.Should().Be(new PageSize(listCaseNoteRequestDto.PageSize));
        listCaseNoteRequest.FromDate.Should().Be(new DateRange(listCaseNoteRequestDto.FromDate));
        listCaseNoteRequest.ToDate.Should().Be(new DateRange(listCaseNoteRequestDto.ToDate));
    }
}