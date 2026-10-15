using Core.Enums;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using Presentation.Models.AuditLogs;

namespace Presentation.Mappers.AuditLogs;

public static class AuditChainRequestMapper
{
    public static ExportAuditLogRequest ToModel(this ExportAuditLogRequestDto dto)
    {
        return new ExportAuditLogRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            EntityType = !string.IsNullOrWhiteSpace(dto.EntityType)
                ? Enum.Parse<AuditableEntityType>(dto.EntityType, true)
                : null,
            EntityId = dto.EntityId,
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static VerifyAuditChainRequest ToModel(this VerifyAuditChainRequestDto dto)
    {
        return new VerifyAuditChainRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static AuditChainVerificationResponseDto ToDto(this AuditChainVerification auditChainVerification)
    {
        return new AuditChainVerificationResponseDto
        {
            OrganizationId = auditChainVerification.OrganizationId,
            EntryCount = auditChainVerification.EntryCount,
            AnchorCount = auditChainVerification.AnchorCount,
            IsIntact = auditChainVerification.IsIntact,
            FirstBrokenAuditLogId = auditChainVerification.FirstBrokenAuditLogId.HasValue
                ? (Guid)auditChainVerification.FirstBrokenAuditLogId.Value
                : null,
            FirstBrokenSequence = auditChainVerification.FirstBrokenSequence.HasValue
                ? (long)auditChainVerification.FirstBrokenSequence.Value
                : null,
            FirstBrokenAuditAnchorId = auditChainVerification.FirstBrokenAuditAnchorId.HasValue
                ? (Guid)auditChainVerification.FirstBrokenAuditAnchorId.Value
                : null,
            VerifiedAt = auditChainVerification.VerifiedAt
        };
    }
}
