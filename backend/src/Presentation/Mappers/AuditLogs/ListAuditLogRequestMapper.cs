using Core.Enums;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using Presentation.Models.AuditLogs;

namespace Presentation.Mappers.AuditLogs;

public static class ListAuditLogRequestMapper
{
    public static ListAuditLogRequest ToModel(this ListAuditLogRequestDto dto, AuditableEntityType entityType, Guid entityId)
    {
        return new ListAuditLogRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            EntityType = entityType,
            EntityId = entityId,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
