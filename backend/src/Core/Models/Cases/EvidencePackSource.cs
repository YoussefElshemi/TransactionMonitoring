using Core.Models.Attachments;
using Core.Models.AuditLogs;
using Core.Models.CaseNotes;
using Core.Models.Decisions;
using Core.Models.RuleEvaluations;

namespace Core.Models.Cases;

public record EvidencePackSource
{
    public required Case Case { get; init; }

    public required EvidencePackPayment? Payment { get; init; }
    public required Decision? Decision { get; init; }
    public required List<CaseNote> Notes { get; init; }
    public required List<Attachment> Attachments { get; init; }
    public required List<RuleEvaluation> RuleEvaluations { get; init; }

    public required List<AuditLog> Timeline { get; init; }

    public required List<EvidencePackActor> Actors { get; init; }
}
