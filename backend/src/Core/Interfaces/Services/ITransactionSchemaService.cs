using System.Text.Json.Nodes;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ITransactionSchemaService : IPagedService<ListTransactionSchemaVersionRequest, TransactionSchemaVersion>
{
    Task<TransactionSchema> SetAsync(SetTransactionSchemaRequest setTransactionSchemaRequest, CancellationToken stoppingToken);
    Task<TransactionSchema> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<TransactionSchema> GetIncludingDeletedAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<TransactionSchemaVersion> GetVersionAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task<Exists> VersionExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task EnforceRuleBodyAsync(OrganizationId organizationId, RuleBodyModel ruleBody, CancellationToken stoppingToken);
    Task EnforcePayloadAsync(OrganizationId organizationId, JsonObject payload, CancellationToken stoppingToken);
    Task EnforceAsync(OrganizationId organizationId, RuleBodyModel ruleBody, JsonObject payload, CancellationToken stoppingToken);
}
