using System.Text.Json.Nodes;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Services;

public class TransactionSchemaService(
    ITransactionSchemaRepository transactionSchemaRepository,
    ITransactionSchemaVersionRepository transactionSchemaVersionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : ITransactionSchemaService
{
    public async Task<TransactionSchema> SetAsync(SetTransactionSchemaRequest setTransactionSchemaRequest, CancellationToken stoppingToken)
    {
        var existing = await transactionSchemaRepository.FindAsync(setTransactionSchemaRequest.OrganizationId, new IncludeDeleted(true), stoppingToken);
        var transactionSchemaVersionId = new TransactionSchemaVersionId(Guid.NewGuid());

        if (existing == null)
        {
            var transactionSchema = setTransactionSchemaRequest.ToModel(transactionSchemaVersionId, timeProvider, modeContext.CurrentOrLive);

            await transactionSchemaRepository.AddAsync(transactionSchema, stoppingToken);
            await transactionSchemaVersionRepository.AddAsync(
                setTransactionSchemaRequest.ToVersionModel(transactionSchemaVersionId, transactionSchema.TransactionSchemaId, transactionSchema.CurrentVersionNumber, timeProvider, modeContext.CurrentOrLive),
                stoppingToken);
        }
        else
        {
            var versionNumber = new VersionNumber(await transactionSchemaVersionRepository.GetMaxVersionNumberAsync(existing.TransactionSchemaId, stoppingToken) + 1);

            await transactionSchemaVersionRepository.AddAsync(
                setTransactionSchemaRequest.ToVersionModel(transactionSchemaVersionId, existing.TransactionSchemaId, versionNumber, timeProvider, modeContext.CurrentOrLive),
                stoppingToken);

            await transactionSchemaRepository.ReplaceFieldsAsync(
                setTransactionSchemaRequest.OrganizationId,
                setTransactionSchemaRequest.Fields,
                transactionSchemaVersionId,
                versionNumber,
                stoppingToken);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        catch (UniqueConstraintViolationException exception)
        {
            // Read before written, so a concurrent Set loses the insert rather than overwriting.
            throw new TransactionSchemaConflictException(setTransactionSchemaRequest.OrganizationId, exception);
        }

        return await GetAsync(setTransactionSchemaRequest.OrganizationId, stoppingToken);
    }

    public async Task<TransactionSchema> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        return transactionSchema ?? throw new InvalidOperationException($"No transaction schema is defined for organisation '{organizationId}'.");
    }

    public async Task<TransactionSchema> GetIncludingDeletedAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(true), stoppingToken);

        return transactionSchema ?? throw new InvalidOperationException($"No transaction schema has ever been defined for organisation '{organizationId}'.");
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return transactionSchemaRepository.ExistsAsync(organizationId, includeDeleted, stoppingToken);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await transactionSchemaRepository.DeleteAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await transactionSchemaRepository.RecoverAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<TransactionSchemaVersion> GetVersionAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var transactionSchema = await GetIncludingDeletedAsync(organizationId, stoppingToken);

        return await transactionSchemaVersionRepository.GetByVersionNumberAsync(transactionSchema.TransactionSchemaId, versionNumber, stoppingToken);
    }

    public Task<Exists> VersionExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        return transactionSchemaVersionRepository.ExistsAsync(organizationId, versionNumber, stoppingToken);
    }

    public async Task EnforceRuleBodyAsync(OrganizationId organizationId, RuleBodyModel ruleBody, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        if (transactionSchema == null)
        {
            return;
        }

        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, transactionSchema);

        if (violations.Count > 0)
        {
            throw new RuleSchemaViolationException(violations);
        }
    }

    public async Task EnforcePayloadAsync(OrganizationId organizationId, JsonObject payload, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        if (transactionSchema == null)
        {
            return;
        }

        var violations = TransactionSchemaHelper.ValidatePayload(payload, transactionSchema);

        if (violations.Count > 0)
        {
            throw new PayloadSchemaViolationException(violations);
        }
    }

    public async Task EnforceAsync(OrganizationId organizationId, RuleBodyModel ruleBody, JsonObject payload, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        if (transactionSchema == null)
        {
            return;
        }

        var ruleBodyViolations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, transactionSchema);

        if (ruleBodyViolations.Count > 0)
        {
            throw new RuleSchemaViolationException(ruleBodyViolations);
        }

        var payloadViolations = TransactionSchemaHelper.ValidatePayload(payload, transactionSchema);

        if (payloadViolations.Count > 0)
        {
            throw new PayloadSchemaViolationException(payloadViolations);
        }
    }

    public Task<PagedResponse<TransactionSchemaVersion>> ListAsync(ListTransactionSchemaVersionRequest listTransactionSchemaVersionRequest, CancellationToken stoppingToken)
    {
        return transactionSchemaVersionRepository.ListAsync(listTransactionSchemaVersionRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListTransactionSchemaVersionRequest listTransactionSchemaVersionRequest, CancellationToken stoppingToken)
    {
        return transactionSchemaVersionRepository.GetCountAsync(listTransactionSchemaVersionRequest, stoppingToken);
    }
}
