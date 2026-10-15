using AutoFixture;
using Core.Enums;
using Core.Models.CaseNotes;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.CaseNotes;

public static class FakeCaseNote
{
    public static CaseNote CreateValid(IFixture fixture)
    {
        return new CaseNote
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            CaseId = FakeCaseId.CreateValid(fixture),
            CaseNoteId = FakeCaseNoteId.CreateValid(fixture),
            Message = FakeMessage.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
        };
    }
}