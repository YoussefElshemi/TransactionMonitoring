using Core.Enums;
using Core.Models.CaseReasonCodes;

namespace Core.Helpers;

public static class CaseClosurePolicy
{
    private static readonly CaseDisposition[] DispositionsRequiringNote =
    [
        CaseDisposition.TruePositive,
        CaseDisposition.FalseNegative
    ];

    public static bool RequiresNote(CaseStatus status, CaseDisposition? disposition)
    {
        return status == CaseStatus.Rejected ||
               (disposition.HasValue && DispositionsRequiringNote.Contains(disposition.Value));
    }

    public static bool PermitsDisposition(this CaseReasonCode caseReasonCode, CaseDisposition disposition)
    {
        return !caseReasonCode.Disposition.HasValue || caseReasonCode.Disposition.Value == disposition;
    }
}
