using Core.Models.AuditLogs;
using Presentation.Mappers.AuditLogs;
using Presentation.Models.AuditLogs;

namespace Presentation.Factories;

public class AuditLogResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<AuditLogResourceDto[]> CreateAsync(
        IReadOnlyCollection<AuditLog> auditLogs, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (auditLogs.Count == 0)
        {
            return [];
        }

        var changedByUsers = await relatedRecordLoader.LoadUsersAsync(
            auditLogs.Select(x => x.ChangedByUserId), context, stoppingToken);
        var changedByApiClients = await relatedRecordLoader.LoadApiClientsAsync(
            auditLogs.Select(x => x.ChangedByApiClientId), context, stoppingToken);

        return auditLogs.Select(x => new AuditLogResourceDto
        {
            AuditLog = x.ToDto(),
            ChangedByUser = x.ChangedByUserId.HasValue ? changedByUsers.GetValueOrDefault(x.ChangedByUserId.Value) : null,
            ChangedByApiClient = x.ChangedByApiClientId.HasValue
                ? changedByApiClients.GetValueOrDefault(x.ChangedByApiClientId.Value)
                : null
        }).ToArray();
    }
}
