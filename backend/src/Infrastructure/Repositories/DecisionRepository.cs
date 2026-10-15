using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Models.Decisions;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DecisionRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IDecisionRepository
{
    public async Task AddAsync(Decision decision, CancellationToken stoppingToken)
    {
        var entry = context.Decisions.Add(decision.ToEntity());

        try
        {
            await context.SaveChangesAsync(stoppingToken);
        }
        catch (DbUpdateException)
        {
            entry.State = EntityState.Detached;

            if (await FindByTransactionAsync(decision.TransactionId, stoppingToken) is null)
            {
                throw;
            }

            throw new DecisionAlreadyRecordedException(decision.TransactionId);
        }
    }

    public async Task<Decision> GetAsync(DecisionId decisionId, CancellationToken stoppingToken)
    {
        var decisionEntity = await Detail()
            .FirstAsync(x => x.DecisionId == decisionId, stoppingToken);

        return ToModel(decisionEntity);
    }

    public async Task<Decision?> FindAsync(DecisionId decisionId, CancellationToken stoppingToken)
    {
        var decisionEntity = await Detail()
            .FirstOrDefaultAsync(x => x.DecisionId == decisionId, stoppingToken);

        return decisionEntity is null ? null : ToModel(decisionEntity);
    }

    public async Task<Decision?> FindByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        var decisionEntity = await Detail()
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId, stoppingToken);

        return decisionEntity is null ? null : ToModel(decisionEntity);
    }

    public async Task<List<Decision>> ResolveByCaseAsync(ResolveDecisionRequest resolveDecisionRequest, CancellationToken stoppingToken)
    {
        var decisionEntities = await Detail()
            .Where(x => x.CaseId == resolveDecisionRequest.CaseId)
            .ToListAsync(stoppingToken);

        var resolved = new List<Decision>();
        var now = timeProvider.GetUtcNow();

        foreach (var decisionEntity in decisionEntities)
        {
            var currentStatus = (DecisionStatus)decisionEntity.StatusId;

            if (currentStatus.IsTerminal() || currentStatus == resolveDecisionRequest.Verdict.Status)
            {
                continue;
            }

            decisionEntity.StatusId = (int)resolveDecisionRequest.Verdict.Status;
            decisionEntity.ReasonCodeId = (int)resolveDecisionRequest.Verdict.ReasonCode;
            decisionEntity.ResolvedByUserId = resolveDecisionRequest.ResolvedByUserId;
            decisionEntity.ResolvedAt = resolveDecisionRequest.Verdict.Status.IsTerminal() ? now : null;
            decisionEntity.UpdatedAt = now;

            context.Entry(decisionEntity).Property(e => e.ResolvedByUserId).IsModified = true;

            context.Decisions.Update(decisionEntity);

            resolved.Add(ToModel(decisionEntity));
        }

        return resolved;
    }

    public async Task<Decision?> ResolveAsync(DecisionId decisionId, DecisionVerdict verdict, ApplicationUserId resolvedByUserId, CancellationToken stoppingToken)
    {
        var decisionEntity = await Detail()
            .FirstOrDefaultAsync(x => x.DecisionId == decisionId, stoppingToken);

        if (decisionEntity is null)
        {
            return null;
        }

        var currentStatus = (DecisionStatus)decisionEntity.StatusId;

        if (currentStatus.IsTerminal() || currentStatus == verdict.Status)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();

        decisionEntity.StatusId = (int)verdict.Status;
        decisionEntity.ReasonCodeId = (int)verdict.ReasonCode;
        decisionEntity.ResolvedByUserId = resolvedByUserId;
        decisionEntity.ResolvedAt = verdict.Status.IsTerminal() ? now : null;
        decisionEntity.UpdatedAt = now;

        context.Entry(decisionEntity).Property(e => e.ResolvedByUserId).IsModified = true;

        context.Decisions.Update(decisionEntity);

        return ToModel(decisionEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, DecisionId decisionId, CancellationToken stoppingToken)
    {
        var exists = await context.Decisions
            .AnyAsync(x => x.DecisionId == decisionId && x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<Decision>> ListDueForExpiryAsync(int batchSize, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var decisionEntities = await Detail()
            .Where(x => x.StatusId == (int)DecisionStatus.Held &&
                        x.HoldExpiryVerdictId != null &&
                        x.HoldExpiryVerdictId != (int)HoldExpiryVerdict.Never &&
                        x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(stoppingToken);

        return decisionEntities.ConvertAll(ToModel);
    }

    public async Task<Dictionary<CaseId, List<DecisionStatus>>> ListStatusesByCaseAsync(
        IReadOnlyCollection<CaseId> caseIds,
        IReadOnlyCollection<DecisionId> settlingDecisionIds,
        CancellationToken stoppingToken)
    {
        var caseIdValues = caseIds.Select(x => (Guid)x).Distinct().ToList();

        if (caseIdValues.Count == 0)
        {
            return [];
        }

        var settlingDecisionIdValues = settlingDecisionIds.Select(x => (Guid)x).ToList();

        var rows = await context.Decisions
            .Where(x => x.CaseId != null &&
                        caseIdValues.Contains(x.CaseId.Value) &&
                        !settlingDecisionIdValues.Contains(x.DecisionId))
            .Select(x => new { CaseId = x.CaseId!.Value, x.StatusId })
            .ToListAsync(stoppingToken);

        return caseIdValues.ToDictionary(
            x => new CaseId(x),
            x => rows.Where(y => y.CaseId == x).Select(y => (DecisionStatus)y.StatusId).ToList());
    }

    public async Task<List<Decision>> ExpireManyAsync(IReadOnlyDictionary<DecisionId, DecisionVerdict> verdicts, CancellationToken stoppingToken)
    {
        if (verdicts.Count == 0)
        {
            return [];
        }

        var decisionIds = verdicts.Keys.Select(x => (Guid)x).ToList();

        var decisionEntities = await Detail()
            .Where(x => decisionIds.Contains(x.DecisionId) && x.StatusId == (int)DecisionStatus.Held)
            .ToListAsync(stoppingToken);

        var now = timeProvider.GetUtcNow();

        foreach (var decisionEntity in decisionEntities)
        {
            var verdict = verdicts[new DecisionId(decisionEntity.DecisionId)];

            decisionEntity.StatusId = (int)verdict.Status;
            decisionEntity.ReasonCodeId = (int)verdict.ReasonCode;
            decisionEntity.ResolvedAt = now;
            decisionEntity.UpdatedAt = now;

            // Update() would attach the decision's case and collide with CaseRepository.ExpireManyAsync's query over the same cases.
            context.Entry(decisionEntity).State = EntityState.Modified;
        }

        return decisionEntities.ConvertAll(ToModel);
    }

    private static Decision ToModel(DecisionEntity decisionEntity)
    {
        var externalReference = decisionEntity.Transaction.ExternalReference is null
            ? (ExternalReference?)null
            : new ExternalReference(decisionEntity.Transaction.ExternalReference);

        return decisionEntity.ToModel(externalReference);
    }

    private IQueryable<DecisionEntity> Detail()
    {
        return context.Decisions
            .Include(x => x.Transaction)
            .Include(x => x.RuleEvaluations.Where(evaluation => !evaluation.IsDeleted));
    }
}
