using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Sandbox;
using Core.Models.Usages;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class SandboxCreditService(
    IUsageService usageService,
    IOrganizationService organizationService,
    ISandboxCreditRepository sandboxCreditRepository,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : ISandboxCreditService
{
    public async Task<SandboxCredit> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organization = await organizationService.GetAsync(organizationId, stoppingToken);
        var allowance = Allowance(organization.SandboxMonthlyCredits);
        var used = await UsedThisMonthAsync(organizationId, stoppingToken);

        return new SandboxCredit
        {
            OrganizationId = organizationId,
            Allowance = allowance,
            Used = used,
            Remaining = new Count(Math.Max(0, allowance - used)),
            ResetsAt = NextReset()
        };
    }

    public Task EnforceAsync(OrganizationId organizationId, BillableResource billableResource, Resource resource, Count cost, CancellationToken stoppingToken)
    {
        if (modeContext.CurrentOrLive != Mode.Test)
        {
            return Task.CompletedTask;
        }

        var charged = Charge(cost);

        // Lock-then-spend as one step, or concurrent callers all read the same balance and overspend.
        return unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await sandboxCreditRepository.LockAsync(organizationId, stoppingToken);

            var credit = await GetAsync(organizationId, stoppingToken);

            ThrowIfShort(charged, credit);

            return await usageService.CreateAsync(new CreateUsageRequest
            {
                Resource = resource,
                BillableResource = billableResource,
                OrganizationId = organizationId,
                Count = charged
            }, stoppingToken);
        }, stoppingToken);
    }

    public async Task EnsureAvailableAsync(OrganizationId organizationId, Count cost, CancellationToken stoppingToken)
    {
        if (modeContext.CurrentOrLive != Mode.Test)
        {
            return;
        }

        ThrowIfShort(Charge(cost), await GetAsync(organizationId, stoppingToken));
    }

    private static void ThrowIfShort(Count charged, SandboxCredit credit)
    {
        if (charged <= credit.Remaining)
        {
            return;
        }

        throw new SandboxCreditsExhaustedException(credit.Allowance, credit.Used, charged, credit.ResetsAt);
    }

    private static Count Charge(Count cost)
    {
        return new Count(Math.Max(1, cost));
    }

    private SandboxCredits Allowance(SandboxCredits? negotiated)
    {
        return negotiated ?? new SandboxCredits(appConfig.Value.SandboxConfig.DefaultMonthlyCredits);
    }

    private async Task<Count> UsedThisMonthAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        using var test = modeContext.Pin(Mode.Test);

        var used = await usageService.SumByDateAsync(new SumUsageByDateRequest
        {
            OrganizationId = organizationId,
            Month = new Month(now.Month),
            Year = new Year(now.Year)
        }, stoppingToken);

        return new Count(used);
    }

    private ResetAt NextReset()
    {
        var now = timeProvider.GetUtcNow();

        return new ResetAt(new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1));
    }
}
