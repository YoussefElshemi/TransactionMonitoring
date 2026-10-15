using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseNotes;

namespace UnitTests.Tests.Core.Mappers;

public class CreateCaseNoteRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateCaseNoteRequest()
    {
        // Arrange
        var createCaseNoteRequest = FakeCreateCaseNoteRequest.CreateValid(Fixture);

        // Act
        var caseNote = createCaseNoteRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        caseNote.CaseNoteId.Should().NotBeNull();
        caseNote.CaseId.Should().Be(new CaseId(createCaseNoteRequest.CaseId));
        caseNote.Message.Should().Be(createCaseNoteRequest.Message);
        caseNote.CreatedByUserId.Should().Be(createCaseNoteRequest.CreatedByUserId);
        caseNote.OrganizationId.Should().Be(createCaseNoteRequest.OrganizationId);
        caseNote.IsDeleted.Should().Be(new IsDeleted(false));
        caseNote.DeletedAt.Should().BeNull();
    }
}