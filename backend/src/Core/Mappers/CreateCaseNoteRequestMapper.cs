using Core.Enums;
using Core.Models.CaseNotes;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateCaseNoteRequestMapper
{
    public static CaseNote ToModel(this CreateCaseNoteRequest createCaseNoteRequest, TimeProvider timeProvider, Mode mode)
    {
        return new CaseNote
        {
            Mode = mode,
            CaseNoteId = new CaseNoteId(Guid.NewGuid()),
            CaseId = new CaseId(createCaseNoteRequest.CaseId),
            Message = createCaseNoteRequest.Message,
            CreatedByUserId = createCaseNoteRequest.CreatedByUserId,
            OrganizationId = createCaseNoteRequest.OrganizationId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}