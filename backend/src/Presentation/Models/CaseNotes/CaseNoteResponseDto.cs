namespace Presentation.Models.CaseNotes;

public class CaseNoteResponseDto
{
    public required Guid CaseNoteId { get; init; }
    public required Guid CaseId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Message { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
