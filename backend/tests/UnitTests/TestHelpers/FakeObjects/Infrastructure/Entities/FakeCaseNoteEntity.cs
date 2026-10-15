using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeCaseNoteEntity
{
    public static CaseNoteEntity CreateValid(IFixture fixture)
    {
        return new CaseNoteEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            CaseId = fixture.Create<Guid>(),
            Case = FakeCaseEntity.CreateValid(fixture),
            CaseNoteId = fixture.Create<Guid>(),
            Message = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture)
        };
    }
}