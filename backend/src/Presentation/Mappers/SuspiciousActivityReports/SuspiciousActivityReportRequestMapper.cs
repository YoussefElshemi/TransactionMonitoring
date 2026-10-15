using Core.Enums;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Mappers.SuspiciousActivityReports;

public static class SuspiciousActivityReportRequestMapper
{
    public static ListSuspiciousActivityReportRequest ToModel(
        this ListSuspiciousActivityReportRequestDto listSuspiciousActivityReportRequestDto)
    {
        return new ListSuspiciousActivityReportRequest
        {
            OrganizationId = new OrganizationId(listSuspiciousActivityReportRequestDto.OrganizationId),
            Status = !string.IsNullOrWhiteSpace(listSuspiciousActivityReportRequestDto.Status)
                ? Enum.Parse<SuspiciousActivityReportStatus>(listSuspiciousActivityReportRequestDto.Status, true)
                : null,
            CaseId = listSuspiciousActivityReportRequestDto.CaseId.HasValue
                ? new CaseId(listSuspiciousActivityReportRequestDto.CaseId.Value)
                : null,
            IsOverdue = listSuspiciousActivityReportRequestDto.IsOverdue.HasValue
                ? new IsBreached(listSuspiciousActivityReportRequestDto.IsOverdue.Value)
                : null,
            PageNumber = new PageNumber(listSuspiciousActivityReportRequestDto.PageNumber),
            PageSize = new PageSize(listSuspiciousActivityReportRequestDto.PageSize),
            FromDate = new DateRange(listSuspiciousActivityReportRequestDto.FromDate),
            ToDate = new DateRange(listSuspiciousActivityReportRequestDto.ToDate)
        };
    }

    public static SetSuspiciousActivityReportPolicyRequest ToModel(
        this SetOrganizationSuspiciousActivityReportPolicyRequestDto setOrganizationSuspiciousActivityReportPolicyRequestDto)
    {
        var body = setOrganizationSuspiciousActivityReportPolicyRequestDto.Body;

        return new SetSuspiciousActivityReportPolicyRequest
        {
            OrganizationId = new OrganizationId(setOrganizationSuspiciousActivityReportPolicyRequestDto.OrganizationId),
            ReportingEntityId = !string.IsNullOrWhiteSpace(body.ReportingEntityId)
                ? new ReportingEntityId(body.ReportingEntityId)
                : null,
            FilingDeadline = body.FilingDeadlineDays.HasValue ? new FilingDeadline(body.FilingDeadlineDays.Value) : null,
            RequireFilingApproval = new IsRequired(body.RequireFilingApproval),
            AmountPath = !string.IsNullOrWhiteSpace(body.AmountPath) ? new PayloadFieldPath(body.AmountPath) : null,
            CurrencyPath = !string.IsNullOrWhiteSpace(body.CurrencyPath) ? new PayloadFieldPath(body.CurrencyPath) : null
        };
    }
}
