using Core.Enums;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using Presentation.Models.AuditLogs;

namespace Presentation.Mappers.AuditLogs;

public static class OrganizationAuditChainRequestMapper
{
    public static ExportAuditLogRequest ToModel(this OrganizationExportAuditLogRequestDto dto)
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

    public static VerifyAuditChainRequest ToModel(this OrganizationVerifyAuditChainRequestDto dto)
    {
        return new VerifyAuditChainRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
