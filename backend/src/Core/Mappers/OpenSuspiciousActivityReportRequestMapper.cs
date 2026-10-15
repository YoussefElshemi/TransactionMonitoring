using Core.Enums;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Mappers;

public static class OpenSuspiciousActivityReportRequestMapper
{
    public static SuspiciousActivityReport ToModel(
        this OpenSuspiciousActivityReportRequest openSuspiciousActivityReportRequest,
        FriendlyName friendlyName,
        Narrative narrative,
        DueAt dueAt,
        TimeProvider timeProvider, Mode mode)
    {
        var now = timeProvider.GetUtcNow();

        return new SuspiciousActivityReport
        {
            Mode = mode,
            SuspiciousActivityReportId = new SuspiciousActivityReportId(Guid.NewGuid()),
            OrganizationId = openSuspiciousActivityReportRequest.OrganizationId,
            CaseId = openSuspiciousActivityReportRequest.CaseId,
            FriendlyName = friendlyName,
            Status = SuspiciousActivityReportStatus.Draft,
            Narrative = narrative,
            DueAt = dueAt,
            OverdueNotifiedAt = null,
            OpenedByUserId = openSuspiciousActivityReportRequest.OpenedByUserId,
            OpenedByUser = null,
            FiledByUserId = null,
            FiledByUser = null,
            FiledAt = null,
            FilingReference = null,
            AcknowledgedAt = null,
            AcknowledgementReference = null,
            NotFiledReason = null,
            DecidedByUserId = null,
            DecidedByUser = null,
            DecidedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
