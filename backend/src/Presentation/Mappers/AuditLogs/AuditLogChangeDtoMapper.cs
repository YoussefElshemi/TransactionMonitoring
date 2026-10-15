using Core.Models.AuditLogs;
using Presentation.Models.AuditLogs;

namespace Presentation.Mappers.AuditLogs;

public static class AuditLogChangeDtoMapper
{
    public static AuditLogChangeDto ToDto(this AuditLogChange change)
    {
        return new AuditLogChangeDto
        {
            Field = change.Field,
            OldValue = change.OldValue,
            NewValue = change.NewValue
        };
    }
}
