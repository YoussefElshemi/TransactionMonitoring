using Core.Enums;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class SuspiciousActivityReportMapper
{
    public static SuspiciousActivityReport ToModel(this SuspiciousActivityReportEntity suspiciousActivityReportEntity)
    {
        return new SuspiciousActivityReport
        {
            Mode = (Mode)suspiciousActivityReportEntity.ModeId,
            SuspiciousActivityReportId = new SuspiciousActivityReportId(suspiciousActivityReportEntity.SuspiciousActivityReportId),
            OrganizationId = new OrganizationId(suspiciousActivityReportEntity.OrganizationId),
            CaseId = new CaseId(suspiciousActivityReportEntity.CaseId),
            FriendlyName = new FriendlyName(suspiciousActivityReportEntity.FriendlyName),
            Status = (SuspiciousActivityReportStatus)suspiciousActivityReportEntity.StatusId,
            Narrative = new Narrative(suspiciousActivityReportEntity.Narrative),
            DueAt = new DueAt(suspiciousActivityReportEntity.DueAt),
            OverdueNotifiedAt = suspiciousActivityReportEntity.OverdueNotifiedAt.HasValue
                ? new OverdueNotifiedAt(suspiciousActivityReportEntity.OverdueNotifiedAt.Value)
                : null,
            OpenedByUserId = new ApplicationUserId(suspiciousActivityReportEntity.OpenedByUserId),
            OpenedByUser = suspiciousActivityReportEntity.OpenedByUser?.ToModel(),
            FiledByUserId = suspiciousActivityReportEntity.FiledByUserId.HasValue
                ? new ApplicationUserId(suspiciousActivityReportEntity.FiledByUserId.Value)
                : null,
            FiledByUser = suspiciousActivityReportEntity.FiledByUser?.ToModel(),
            FiledAt = suspiciousActivityReportEntity.FiledAt.HasValue ? new FiledAt(suspiciousActivityReportEntity.FiledAt.Value) : null,
            FilingReference = !string.IsNullOrWhiteSpace(suspiciousActivityReportEntity.FilingReference)
                ? new FilingReference(suspiciousActivityReportEntity.FilingReference)
                : null,
            AcknowledgedAt = suspiciousActivityReportEntity.AcknowledgedAt.HasValue
                ? new AcknowledgedAt(suspiciousActivityReportEntity.AcknowledgedAt.Value)
                : null,
            AcknowledgementReference = !string.IsNullOrWhiteSpace(suspiciousActivityReportEntity.AcknowledgementReference)
                ? new FilingReference(suspiciousActivityReportEntity.AcknowledgementReference)
                : null,
            NotFiledReason = !string.IsNullOrWhiteSpace(suspiciousActivityReportEntity.NotFiledReason)
                ? new Message(suspiciousActivityReportEntity.NotFiledReason)
                : null,
            DecidedByUserId = suspiciousActivityReportEntity.DecidedByUserId.HasValue
                ? new ApplicationUserId(suspiciousActivityReportEntity.DecidedByUserId.Value)
                : null,
            DecidedByUser = suspiciousActivityReportEntity.DecidedByUser?.ToModel(),
            DecidedAt = suspiciousActivityReportEntity.DecidedAt.HasValue
                ? new DecidedAt(suspiciousActivityReportEntity.DecidedAt.Value)
                : null,
            CreatedAt = new CreatedAt(suspiciousActivityReportEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(suspiciousActivityReportEntity.UpdatedAt)
        };
    }

    public static SuspiciousActivityReportEntity ToEntity(this SuspiciousActivityReport suspiciousActivityReport)
    {
        return new SuspiciousActivityReportEntity
        {
            ModeId = (int)suspiciousActivityReport.Mode,
            SuspiciousActivityReportId = suspiciousActivityReport.SuspiciousActivityReportId,
            OrganizationId = suspiciousActivityReport.OrganizationId,
            CaseId = suspiciousActivityReport.CaseId,
            FriendlyName = suspiciousActivityReport.FriendlyName,
            StatusId = (int)suspiciousActivityReport.Status,
            Narrative = suspiciousActivityReport.Narrative,
            DueAt = suspiciousActivityReport.DueAt,
            OverdueNotifiedAt = suspiciousActivityReport.OverdueNotifiedAt.HasValue
                ? suspiciousActivityReport.OverdueNotifiedAt.Value
                : null,
            OpenedByUserId = suspiciousActivityReport.OpenedByUserId,
            FiledByUserId = suspiciousActivityReport.FiledByUserId.HasValue ? suspiciousActivityReport.FiledByUserId.Value : null,
            FiledAt = suspiciousActivityReport.FiledAt.HasValue ? suspiciousActivityReport.FiledAt.Value : null,
            FilingReference = suspiciousActivityReport.FilingReference?.ToString(),
            AcknowledgedAt = suspiciousActivityReport.AcknowledgedAt.HasValue ? suspiciousActivityReport.AcknowledgedAt.Value : null,
            AcknowledgementReference = suspiciousActivityReport.AcknowledgementReference?.ToString(),
            NotFiledReason = suspiciousActivityReport.NotFiledReason?.ToString(),
            DecidedByUserId = suspiciousActivityReport.DecidedByUserId.HasValue ? suspiciousActivityReport.DecidedByUserId.Value : null,
            DecidedAt = suspiciousActivityReport.DecidedAt.HasValue ? suspiciousActivityReport.DecidedAt.Value : null,
            CreatedAt = suspiciousActivityReport.CreatedAt,
            UpdatedAt = suspiciousActivityReport.UpdatedAt
        };
    }
}
