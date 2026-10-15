using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Cases;
using Core.ValueObjects;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CaseSlaTargetRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider,
    IModeContext modeContext) : ICaseSlaTargetRepository
{
    public async Task<Dictionary<RuleSeverity, SlaTarget>> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var caseSlaTargetEntities = await context.CaseSlaTargets
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync(stoppingToken);

        return caseSlaTargetEntities.ToDictionary(x => (RuleSeverity)x.SeverityId, x => new SlaTarget(x.TargetMinutes));
    }

    public async Task SetAsync(SetCaseSlaTargetsRequest setCaseSlaTargetsRequest, CancellationToken stoppingToken)
    {
        var existing = await context.CaseSlaTargets
            .Where(x => x.OrganizationId == setCaseSlaTargetsRequest.OrganizationId)
            .ToListAsync(stoppingToken);

        context.CaseSlaTargets.RemoveRange(existing);

        var now = timeProvider.GetUtcNow();

        var caseSlaTargetEntities = setCaseSlaTargetsRequest.Targets.Select(x => new CaseSlaTargetEntity
        {
            ModeId = (int)modeContext.CurrentOrLive,
            OrganizationId = setCaseSlaTargetsRequest.OrganizationId,
            SeverityId = (int)x.Key,
            TargetMinutes = x.Value,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        await context.CaseSlaTargets.AddRangeAsync(caseSlaTargetEntities, stoppingToken);
    }
}
