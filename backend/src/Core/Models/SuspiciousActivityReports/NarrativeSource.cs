using Core.Models.CaseNotes;
using Core.Models.Cases;
using Core.Models.RuleEvaluations;
using Core.Models.Transactions;

namespace Core.Models.SuspiciousActivityReports;

public record NarrativeSource
{
    public required Case Case { get; init; }

    public required Transaction? Transaction { get; init; }

    public required List<CaseNote> Notes { get; init; }

    public required List<RuleEvaluation> RuleEvaluations { get; init; }
    public required List<NarrativePartyHistory> Parties { get; init; }
}
