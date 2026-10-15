using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseNotes;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class CaseNoteMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CaseNote()
    {
        // Arrange
        var caseNoteEntity = FakeCaseNoteEntity.CreateValid(Fixture);

        // Act
        var caseNote = caseNoteEntity.ToModel();

        // Assert
        caseNote.CaseNoteId.Should().Be(new CaseNoteId(caseNoteEntity.CaseNoteId));
        caseNote.CaseId.Should().Be(new CaseId(caseNoteEntity.CaseId));
        caseNote.Message.Should().Be(new Message(caseNoteEntity.Message));
        caseNote.CreatedByUserId.Should().Be(new ApplicationUserId(caseNoteEntity.CreatedByUserId));
        caseNote.OrganizationId.Should().Be(new OrganizationId(caseNoteEntity.OrganizationId));
        caseNote.CreatedAt.Should().Be(new CreatedAt(caseNoteEntity.CreatedAt));
        caseNote.UpdatedAt.Should().Be(new UpdatedAt(caseNoteEntity.UpdatedAt));
        caseNote.IsDeleted.Should().Be(new IsDeleted(caseNoteEntity.IsDeleted));
        caseNote.DeletedAt.Should().Be(new DeletedAt(caseNoteEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_CaseNote()
    {
        // Arrange
        var caseNote = FakeCaseNote.CreateValid(Fixture);

        // Act
        var caseNoteEntity = caseNote.ToEntity();

        // Assert
        caseNoteEntity.CaseNoteId.Should().Be(caseNote.CaseNoteId);
        caseNoteEntity.CaseId.Should().Be(caseNote.CaseId);
        caseNoteEntity.Message.Should().Be(caseNote.Message);
        caseNoteEntity.CreatedByUserId.Should().Be(caseNote.CreatedByUserId);
        caseNoteEntity.OrganizationId.Should().Be(caseNote.OrganizationId);
        caseNoteEntity.CreatedAt.Should().Be(caseNote.CreatedAt);
        caseNoteEntity.UpdatedAt.Should().Be(caseNote.UpdatedAt);
        caseNoteEntity.IsDeleted.Should().Be(caseNote.IsDeleted);
        caseNoteEntity.DeletedAt.Should().Be(caseNote.DeletedAt!.Value);
    }
}