using System.Text.Json.Nodes;
using Core.Enums;
using Core.Extensions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.AuditLogs;
using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Escalations;
using Core.Models.Queues;
using Core.Models.Statistics;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Helpers;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CaseRepository(
    TransactionMonitoringDbContext context,
    IUnitOfWork unitOfWork,
    IPayloadProtectionService payloadProtectionService,
    TimeProvider timeProvider) : PagedRepository<ListCaseRequest, CaseEntity, Case>, ICaseRepository
{
    private const int MaxClaimAttempts = 5;

    public async Task AddAsync(Case @case, CancellationToken stoppingToken)
    {
        var caseEntity = @case.ToEntity();
        await context.Cases.AddAsync(caseEntity, stoppingToken);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Cases
            .AnyAsync(x => x.CaseId == caseId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Case> GetAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var caseEntity = await Detail()
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        return caseEntity.ToModel();
    }

    public async Task AssignAsync(CaseId caseId, ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        caseEntity.StatusId = (int)CaseStatus.Assigned;
        caseEntity.AssignedToUserId = userId;
        caseEntity.AssignedAt = timeProvider.GetUtcNow();
        caseEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Entry(caseEntity).Property(e => e.AssignedToUserId).IsModified = true;

        context.Cases.Update(caseEntity);
    }

    // Owns its own save and retries rather than locking: the row version refuses the losing claim, which moves on to the next case. Bounded.
    public async Task<Case?> ClaimNextAsync(ClaimNextRequest claimNextRequest, CancellationToken stoppingToken)
    {
        for (var attempt = 0; attempt < MaxClaimAttempts; attempt++)
        {
            var caseEntity = await ClaimableQuery(claimNextRequest)
                .OrderByDescending(x => x.SeverityId)
                .ThenBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(stoppingToken);

            if (caseEntity is null)
            {
                return null;
            }

            caseEntity.StatusId = (int)CaseStatus.Assigned;
            caseEntity.AssignedToUserId = claimNextRequest.UserId;
            caseEntity.AssignedAt = timeProvider.GetUtcNow();
            caseEntity.UpdatedAt = timeProvider.GetUtcNow();

            context.Entry(caseEntity).Property(e => e.AssignedToUserId).IsModified = true;

            context.Cases.Update(caseEntity);

            try
            {
                await unitOfWork.SaveChangesAsync(stoppingToken);

                return await GetAsync(new CaseId(caseEntity.CaseId), stoppingToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Clear the whole tracker: Update() marked the auto-included graph Modified with stale row versions.
                context.ChangeTracker.Clear();
            }
        }

        return null;
    }

    public async Task UnassignAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        caseEntity.StatusId = (int)CaseStatus.Pending;
        caseEntity.AssignedToUserId = null;
        caseEntity.AssignedAt = null;
        caseEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Entry(caseEntity).Property(e => e.AssignedToUserId).IsModified = true;

        context.Cases.Update(caseEntity);
    }

    public async Task<Count> ReassignOpenCasesAsync(
        OrganizationId organizationId, ApplicationUserId fromUserId, ApplicationUserId? toUserId, CancellationToken stoppingToken)
    {
        Guid fromUserIdValue = fromUserId;
        var now = timeProvider.GetUtcNow();

        // Load-bearing: ExecuteUpdate cannot translate a query with the auto-included joins.
        var queryable = context.Cases
            .IgnoreAutoIncludes()
            .Where(x => x.OrganizationId == organizationId &&
                        x.AssignedToUserId == fromUserIdValue &&
                        !x.IsDeleted &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId));

        int updated;

        if (toUserId is { } successorUserId)
        {
            Guid successorUserIdValue = successorUserId;

            updated = await queryable.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.AssignedToUserId, successorUserIdValue)
                    .SetProperty(x => x.AssignedAt, now)
                    .SetProperty(x => x.UpdatedAt, now),
                stoppingToken);
        }
        else
        {
            updated = await queryable.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.AssignedToUserId, (Guid?)null)
                    .SetProperty(x => x.AssignedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.StatusId, (int)CaseStatus.Pending)
                    .SetProperty(x => x.UpdatedAt, now),
                stoppingToken);
        }

        return new Count(updated);
    }

    public async Task<Case> ResolveAsync(CaseId caseId, CaseClosure caseClosure, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        caseEntity.StatusId = (int)caseClosure.Status;
        caseEntity.DispositionId = caseClosure.Disposition.HasValue ? (int)caseClosure.Disposition.Value : null;
        caseEntity.CaseReasonCodeId = caseClosure.CaseReasonCodeId;
        caseEntity.ResolvedAt = timeProvider.GetUtcNow();
        caseEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Cases.Update(caseEntity);

        return caseEntity.ToModel();
    }

    public async Task<Case> ReopenAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        caseEntity.StatusId = caseEntity.AssignedToUserId.HasValue
            ? (int)CaseStatus.InReview
            : (int)CaseStatus.Pending;

        caseEntity.DispositionId = null;
        caseEntity.CaseReasonCodeId = null;
        caseEntity.ResolvedAt = null;
        caseEntity.ReopenedAt = timeProvider.GetUtcNow();
        caseEntity.UpdatedAt = timeProvider.GetUtcNow();

        // Update(), not Entry(): Entry would attach the case alone and leave the assigned user untracked for the webhooks.
        context.Cases.Update(caseEntity);

        return caseEntity.ToModel();
    }

    public async Task<Case> ChangeStatusAsync(CaseId caseId, CaseStatus status, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        caseEntity.StatusId = (int)status;
        caseEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Cases.Update(caseEntity);

        return caseEntity.ToModel();
    }

    public async Task<Dictionary<CaseStatus, TotalCount>> CountByStatusAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken)
    {
        var counts = await Filter(listCaseRequest)
            .GroupBy(x => x.StatusId)
            .Select(x => new { StatusId = x.Key, Count = x.Count() })
            .ToListAsync(stoppingToken);

        var countsByStatusId = counts.ToDictionary(x => x.StatusId, x => x.Count);

        return Enum.GetValues<CaseStatus>()
            .ToDictionary(x => x, x => new TotalCount(countsByStatusId.GetValueOrDefault((int)x)));
    }

    public async Task<TotalCount> CountBreachedAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var count = await Filter(listCaseRequest)
            .CountAsync(x => x.DueAt < (x.ResolvedAt ?? now), stoppingToken);

        return new TotalCount(count);
    }

    public async Task<List<Case>> ListBreachNotificationCandidatesAsync(Count batchSize, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var caseEntities = await context.Cases
            .IgnoreAutoIncludes()
            .Where(x => !x.IsDeleted &&
                        x.BreachNotifiedAt == null &&
                        x.DueAt < now &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId))
            .OrderBy(x => x.DueAt)
            .ThenBy(x => x.CaseId)
            .Take(batchSize)
            .ToListAsync(stoppingToken);

        return caseEntities.ConvertAll(x => x.ToModel());
    }

    public async Task MarkBreachNotifiedAsync(List<CaseId> caseIds, CancellationToken stoppingToken)
    {
        var ids = caseIds.ConvertAll(x => (Guid)x);

        var caseEntities = await context.Cases
            .IgnoreAutoIncludes()
            .Where(x => ids.Contains(x.CaseId))
            .ToListAsync(stoppingToken);

        foreach (var caseEntity in caseEntities)
        {
            caseEntity.BreachNotifiedAt = timeProvider.GetUtcNow();
            caseEntity.UpdatedAt = timeProvider.GetUtcNow();

            context.Cases.Update(caseEntity);
        }
    }

    public async Task<List<EscalationCandidate>> ListEscalationCandidatesAsync(
        ListEscalationCandidateRequest listEscalationCandidateRequest,
        CancellationToken stoppingToken)
    {
        var queryable = context.Cases
            .IgnoreAutoIncludes()
            .Where(x => x.OrganizationId == listEscalationCandidateRequest.OrganizationId &&
                        !x.IsDeleted &&
                        x.EscalatedAt == null &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId));

        if (listEscalationCandidateRequest.AfterCreatedAt.HasValue && listEscalationCandidateRequest.AfterCaseId.HasValue)
        {
            DateTimeOffset afterCreatedAt = listEscalationCandidateRequest.AfterCreatedAt.Value;
            Guid afterCaseId = listEscalationCandidateRequest.AfterCaseId.Value;

            queryable = queryable.Where(x => x.CreatedAt > afterCreatedAt ||
                                             (x.CreatedAt == afterCreatedAt && x.CaseId.CompareTo(afterCaseId) > 0));
        }

        var candidates = await queryable
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.CaseId)
            .Take(listEscalationCandidateRequest.BatchSize)
            .Select(x => new
            {
                Case = x,
                Payload = x.Transaction != null ? x.Transaction.Payload : null,

                HoldExpiresAt = context.Decisions
                    .Where(y => y.CaseId == x.CaseId &&
                                y.StatusId == (int)DecisionStatus.Held &&
                                y.ExpiresAt != null)
                    .Min(y => y.ExpiresAt)
            })
            .ToListAsync(stoppingToken);

        var dataKeys = await payloadProtectionService.FindDataKeysAsync(
            candidates.ConvertAll(x => new OrganizationId(x.Case.OrganizationId)), stoppingToken);

        return candidates.ConvertAll(x =>
        {
            var organizationId = new OrganizationId(x.Case.OrganizationId);

            return new EscalationCandidate
            {
                Case = x.Case.ToModel(),

                Payload = x.Payload is not null
                    ? PayloadProtector.Read(dataKeys.TryGetValue(organizationId, out var dataKey) ? dataKey : null, organizationId, x.Payload)
                    : new JsonObject(),
                HoldExpiresAt = x.HoldExpiresAt.HasValue ? new ExpiresAt(x.HoldExpiresAt.Value) : null
            };
        });
    }

    public async Task<JsonObject> GetPayloadAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var row = await context.Cases
            .IgnoreAutoIncludes()
            .Where(x => x.CaseId == caseId)
            .Select(x => new { x.OrganizationId, Payload = x.Transaction != null ? x.Transaction.Payload : null })
            .FirstOrDefaultAsync(stoppingToken);

        if (row?.Payload is null)
        {
            return new JsonObject();
        }

        var organizationId = new OrganizationId(row.OrganizationId);
        var dataKey = await payloadProtectionService.FindDataKeyAsync(organizationId, stoppingToken);

        return PayloadProtector.Read(dataKey, organizationId, row.Payload);
    }

    public async Task<EvidencePackSource> GetEvidencePackSourceAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var caseEntity = await Detail().FirstAsync(x => x.CaseId == caseId, stoppingToken);

        var transactionEntity = caseEntity.TransactionId.HasValue
            ? await context.Transactions
                .IgnoreAutoIncludes()
                .FirstOrDefaultAsync(x => x.TransactionId == caseEntity.TransactionId.Value, stoppingToken)
            : null;

        var decisionEntities = await context.Decisions
            .IgnoreAutoIncludes()
            .Where(x => x.CaseId == (Guid)caseId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.DecisionId)
            .ToListAsync(stoppingToken);

        var decisionEntity = decisionEntities.FirstOrDefault();

        var caseNoteEntities = await context.CaseNotes
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        var attachmentEntities = await context.Attachments
            .IgnoreAutoIncludes()
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        // Walked from the evaluations: projecting the navigation off the link table yields an entity type EF refuses to Include onto.
        var ruleEvaluationEntities = await context.RuleEvaluations
            .Include(x => x.Rule)
            .Include(x => x.RuleVersion)
            .Where(x => x.CaseRuleEvaluationLinks.Any(y => y.CaseId == caseId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        var auditLogEntities = await ListTimelineAsync(
            caseEntity, decisionEntities, caseNoteEntities, attachmentEntities, stoppingToken);

        var timeline = auditLogEntities.ToTimeline();

        return new EvidencePackSource
        {
            Case = caseEntity.ToModel(),
            Payment = transactionEntity is not null
                ? new EvidencePackPayment
                {
                    TransactionId = new TransactionId(transactionEntity.TransactionId),
                    FriendlyName = new FriendlyName(transactionEntity.FriendlyName),
                    ExternalReference = transactionEntity.ExternalReference is not null
                        ? new ExternalReference(transactionEntity.ExternalReference)
                        : null,
                    CreatedAt = new CreatedAt(transactionEntity.CreatedAt)
                }
                : null,
            Decision = decisionEntity?.ToModel(transactionEntity?.ExternalReference is not null
                ? new ExternalReference(transactionEntity.ExternalReference)
                : null),
            Notes = caseNoteEntities.Where(x => !x.IsDeleted).Select(x => x.ToModel()).ToList(),
            Attachments = attachmentEntities.Where(x => !x.IsDeleted).Select(x => x.ToModel()).ToList(),
            RuleEvaluations = ruleEvaluationEntities.ConvertAll(x => x.ToModel()),
            Timeline = timeline,
            Actors = await ListActorsAsync(caseEntity, decisionEntities, caseNoteEntities, attachmentEntities, timeline, stoppingToken)
        };
    }

    private async Task<List<AuditLogEntity>> ListTimelineAsync(
        CaseEntity caseEntity,
        List<DecisionEntity> decisionEntities,
        List<CaseNoteEntity> caseNoteEntities,
        List<AttachmentEntity> attachmentEntities,
        CancellationToken stoppingToken)
    {
        var entityIds = new List<Guid> { caseEntity.CaseId };

        entityIds.AddRange(caseNoteEntities.Select(x => x.CaseNoteId));
        entityIds.AddRange(attachmentEntities.Select(x => x.AttachmentId));
        entityIds.AddRange(decisionEntities.Select(x => x.DecisionId));

        var entityTypeIds = new List<int>
        {
            (int)AuditableEntityType.Case,
            (int)AuditableEntityType.CaseNote,
            (int)AuditableEntityType.Attachment,
            (int)AuditableEntityType.Decision
        };

        return await context.AuditLogs
            .Where(x => x.OrganizationId == caseEntity.OrganizationId &&
                        entityTypeIds.Contains(x.EntityTypeId) &&
                        entityIds.Contains(x.EntityId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);
    }

    private async Task<List<EvidencePackActor>> ListActorsAsync(
        CaseEntity caseEntity,
        List<DecisionEntity> decisionEntities,
        List<CaseNoteEntity> caseNoteEntities,
        List<AttachmentEntity> attachmentEntities,
        List<AuditLog> timeline,
        CancellationToken stoppingToken)
    {
        var userIds = new List<Guid>();

        if (caseEntity.AssignedToUserId.HasValue)
        {
            userIds.Add(caseEntity.AssignedToUserId.Value);
        }

        userIds.AddRange(decisionEntities
            .Where(x => x.ResolvedByUserId.HasValue)
            .Select(x => x.ResolvedByUserId!.Value));

        userIds.AddRange(caseNoteEntities.Select(x => x.CreatedByUserId));
        userIds.AddRange(attachmentEntities.Select(x => x.UploadedByUserId));
        userIds.AddRange(timeline.Where(x => x.ChangedByUserId.HasValue).Select(x => (Guid)x.ChangedByUserId!.Value));

        var distinct = userIds.Distinct().ToList();

        if (distinct.Count == 0)
        {
            return [];
        }

        var actors = await context.Users
            .Where(x => distinct.Contains(x.Id))
            .Select(x => new { x.Id, x.UserName, x.Email })
            .ToListAsync(stoppingToken);

        return actors.ConvertAll(x => new EvidencePackActor
        {
            UserId = new ApplicationUserId(x.Id),
            UserName = x.UserName is not null ? new UserName(x.UserName) : null,
            Email = x.Email is not null ? new Email(x.Email) : null
        });
    }

    public async Task<List<Case>> EscalateManyAsync(IReadOnlyCollection<CaseEscalation> caseEscalations, CancellationToken stoppingToken)
    {
        if (caseEscalations.Count == 0)
        {
            return [];
        }

        var escalationsByCaseId = caseEscalations.ToDictionary(x => (Guid)x.CaseId);
        var caseIdValues = escalationsByCaseId.Keys.ToList();

        var caseEntities = await context.Cases
            .Where(x => caseIdValues.Contains(x.CaseId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        foreach (var caseEntity in caseEntities)
        {
            var caseEscalation = escalationsByCaseId[caseEntity.CaseId];

            caseEntity.EscalatedAt = caseEscalation.EscalatedAt;
            caseEntity.EscalationRuleId = caseEscalation.EscalationRuleId;

            if (caseEscalation.TargetQueueId.HasValue)
            {
                caseEntity.QueueId = caseEscalation.TargetQueueId.Value;
            }

            if (caseEscalation.RaiseSeverityTo.HasValue)
            {
                caseEntity.SeverityId = (int)caseEscalation.RaiseSeverityTo.Value;
            }

            caseEntity.UpdatedAt = caseEscalation.EscalatedAt;

            context.Cases.Update(caseEntity);
        }

        return caseEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<Case>> ExpireManyAsync(IReadOnlyCollection<CaseId> caseIds, CancellationToken stoppingToken)
    {
        if (caseIds.Count == 0)
        {
            return [];
        }

        var caseIdValues = caseIds.Select(x => (Guid)x).ToList();

        var caseEntities = await context.Cases
            .Where(x => caseIdValues.Contains(x.CaseId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        var now = timeProvider.GetUtcNow();

        foreach (var caseEntity in caseEntities)
        {
            caseEntity.StatusId = (int)CaseStatus.Expired;
            caseEntity.ResolvedAt = now;
            caseEntity.UpdatedAt = now;

            context.Cases.Update(caseEntity);
        }

        return caseEntities.ConvertAll(x => x.ToModel());
    }

    public async Task DeleteAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId && !x.IsDeleted, stoppingToken);

        caseEntity.IsDeleted = true;
        caseEntity.DeletedAt = timeProvider.GetUtcNow();

        context.Cases.Update(caseEntity);
    }

    public async Task RecoverAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .FirstAsync(x => x.CaseId == caseId, stoppingToken);

        caseEntity.IsDeleted = false;
        caseEntity.DeletedAt = null;

        context.Cases.Update(caseEntity);
    }

    public async Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken)
    {
        var result = await context.Database
            .SqlQueryRaw<int>($"SELECT nextval('\"{Schemas.Service}\".\"{Sequences.CaseNumber}\"') as \"Value\"")
            .FirstAsync(stoppingToken);

        return new FriendlyName("CA" + result.ToString("D10"));
    }

    public async Task<TimeSpan?> GetAverageResolutionTimeAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken)
    {
        var queryable = context.Cases
            .Where(x => x.OrganizationId == statisticRequest.OrganizationId)
            .Where(c => new List<int> { (int)CaseStatus.Approved, (int)CaseStatus.Rejected }.Contains(c.StatusId));

        queryable = FilterByDateRange(queryable, statisticRequest);

        var averageDuration = await queryable
            .Select(x => x.ResolvedAt - x.AssignedAt)
            .DefaultIfEmpty()
            .AverageAsync(x => (double?)x!.Value.TotalMilliseconds, stoppingToken) ?? null;

        return averageDuration is not null ? TimeSpan.FromTicks((long)averageDuration) : null;
    }

    private IQueryable<CaseEntity> ClaimableQuery(ClaimNextRequest claimNextRequest)
    {
        Guid userId = claimNextRequest.UserId;

        var queryable = context.Cases
            .Where(x => x.OrganizationId == claimNextRequest.OrganizationId &&
                        !x.IsDeleted &&
                        x.StatusId == (int)CaseStatus.Pending &&
                        x.AssignedToUserId == null &&
                        x.Queue != null &&
                        x.Queue.IsEnabled &&
                        !x.Queue.IsDeleted);

        return claimNextRequest.QueueId.HasValue
            ? queryable.Where(x => x.QueueId == claimNextRequest.QueueId.Value)
            : queryable.Where(x => x.Queue!.Members.Any(y => y.UserId == userId));
    }

    protected override Task<List<Case>> MapPageAsync(List<CaseEntity> items, ListCaseRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    // Not auto-included: Update() would drag every alert into the tracker and the next repository couldn't attach its own copies.
    private IQueryable<CaseEntity> Detail()
    {
        return context.Cases.Include(x => x.Alerts).Include(x => x.ReasonCode).Include(x => x.Queue);
    }

    protected override IOrderByExpression<CaseEntity> OrderByExpression()
    {
        return new OrderByExpression<CaseEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<CaseEntity> FilterByPagedRequest(ListCaseRequest listCaseRequest)
    {
        var queryable = context.Cases
            .Include(x => x.Alerts)
            .Include(x => x.ReasonCode)
            .Include(x => x.Queue)
            .Where(x => x.OrganizationId == listCaseRequest.OrganizationId && !x.IsDeleted)
            .ApplyPermissionFilter(listCaseRequest.PermissionFilter);

        if (listCaseRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listCaseRequest.FriendlyName}%"));
        }

        if (listCaseRequest.Query.HasValue)
        {
            var pattern = LikePattern.Containing(listCaseRequest.Query.Value);

            var matchingPartyIds = context.Parties
                .Where(x => x.OrganizationId == listCaseRequest.OrganizationId &&
                            (EF.Functions.ILike(x.Identifier, pattern, LikePattern.EscapeCharacter) ||
                             (x.DisplayName != null && EF.Functions.ILike(x.DisplayName, pattern, LikePattern.EscapeCharacter))))
                .Select(x => (Guid?)x.PartyId);

            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, pattern, LikePattern.EscapeCharacter) ||
                            matchingPartyIds.Contains(x.PartyId));
        }

        if (listCaseRequest.Status.HasValue)
        {
            queryable = queryable
                .Where(x => x.StatusId == (int)listCaseRequest.Status.Value);
        }

        if (listCaseRequest.Disposition.HasValue)
        {
            queryable = queryable
                .Where(x => x.DispositionId == (int)listCaseRequest.Disposition.Value);
        }

        if (listCaseRequest.Severity.HasValue)
        {
            queryable = queryable
                .Where(x => x.SeverityId == (int)listCaseRequest.Severity.Value);
        }

        if (listCaseRequest.AssignedToUserId.HasValue)
        {
            queryable = queryable
                .Where(x => x.AssignedToUserId == listCaseRequest.AssignedToUserId.Value);
        }

        if (listCaseRequest.QueueId.HasValue)
        {
            queryable = queryable
                .Where(x => x.QueueId == listCaseRequest.QueueId.Value);
        }

        if (listCaseRequest.Unrouted.HasValue)
        {
            queryable = listCaseRequest.Unrouted.Value
                ? queryable.Where(x => x.QueueId == null)
                : queryable.Where(x => x.QueueId != null);
        }

        if (listCaseRequest.QueueMemberUserId.HasValue)
        {
            Guid queueMemberUserId = listCaseRequest.QueueMemberUserId.Value;

            queryable = queryable
                .Where(x => x.Queue != null && x.Queue.Members.Any(y => y.UserId == queueMemberUserId));
        }

        if (listCaseRequest.Breached.HasValue)
        {
            var now = timeProvider.GetUtcNow();

            queryable = listCaseRequest.Breached.Value
                ? queryable.Where(x => x.DueAt < (x.ResolvedAt ?? now))
                : queryable.Where(x => x.DueAt >= (x.ResolvedAt ?? now));
        }

        if (listCaseRequest.DueBefore.HasValue)
        {
            DateTimeOffset dueBefore = listCaseRequest.DueBefore.Value;

            queryable = queryable.Where(x => x.DueAt < dueBefore);
        }

        if (listCaseRequest.Escalated.HasValue)
        {
            queryable = listCaseRequest.Escalated.Value
                ? queryable.Where(x => x.EscalatedAt != null)
                : queryable.Where(x => x.EscalatedAt == null);
        }

        if (listCaseRequest.PartyId.HasValue)
        {
            queryable = queryable
                .Where(x => x.PartyId == listCaseRequest.PartyId.Value);
        }

        return queryable;
    }

    protected override IQueryable<CaseEntity> FilterByDateRange(IQueryable<CaseEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}