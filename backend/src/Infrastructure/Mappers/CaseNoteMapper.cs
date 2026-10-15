using Core.Enums;
using Core.Models.CaseNotes;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CaseNoteMapper
{
    public static CaseNote ToModel(this CaseNoteEntity caseNoteEntity)
    {
        return new CaseNote
        {
            Mode = (Mode)caseNoteEntity.ModeId,
            CaseNoteId = new CaseNoteId(caseNoteEntity.CaseNoteId),
            CaseId = new CaseId(caseNoteEntity.CaseId),
            Message = new Message(caseNoteEntity.Message),
            CreatedByUserId = new ApplicationUserId(caseNoteEntity.CreatedByUserId),
            OrganizationId = new OrganizationId(caseNoteEntity.OrganizationId),
            CreatedAt = new CreatedAt(caseNoteEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(caseNoteEntity.UpdatedAt),
            IsDeleted = new IsDeleted(caseNoteEntity.IsDeleted),
            DeletedAt = caseNoteEntity.DeletedAt.HasValue ? new DeletedAt(caseNoteEntity.DeletedAt.Value) : null
        };
    }

    public static CaseNoteEntity ToEntity(this CaseNote caseNote)
    {
        return new CaseNoteEntity
        {
            ModeId = (int)caseNote.Mode,
            CaseNoteId = caseNote.CaseNoteId,
            CaseId = caseNote.CaseId,
            Message = caseNote.Message,
            CreatedByUserId = caseNote.CreatedByUserId,
            OrganizationId = caseNote.OrganizationId,
            CreatedAt = caseNote.CreatedAt,
            UpdatedAt = caseNote.UpdatedAt,
            IsDeleted = caseNote.IsDeleted,
            DeletedAt = caseNote.DeletedAt
        };
    }
}