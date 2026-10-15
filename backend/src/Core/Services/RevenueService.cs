using Core.Configs;
using Core.Documents;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Revenue;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class RevenueService(
    IInvoiceRepository invoiceRepository,
    ICreditNoteRepository creditNoteRepository,
    IDunningRepository dunningRepository,
    IUsageRepository usageRepository,
    IAvailabilityWindowRepository availabilityWindowRepository,
    IOrganizationRepository organizationRepository,
    IServiceCostRateService serviceCostRateService,
    IExchangeRateService exchangeRateService,
    IBillingEntityService billingEntityService,
    IModeContext modeContext,
    TimeProvider timeProvider,
    IOptions<AppConfig> appConfig) : IRevenueService
{
    private RevenueConfig RevenueConfig => appConfig.Value.RevenueConfig;

    public async Task<List<RecurringRevenueMonth>> ListRecurringRevenueAsync(
        ListRecurringRevenueRequest listRecurringRevenueRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var firstMonthKey = BillingPeriod.Key(listRecurringRevenueRequest.FromMonth, listRecurringRevenueRequest.FromYear);
        var lastMonthKey = BillingPeriod.Key(listRecurringRevenueRequest.ToMonth, listRecurringRevenueRequest.ToYear);

        var billedMonths = await invoiceRepository.GetBilledMonthsAsync(0, lastMonthKey, stoppingToken);

        var revenueByMonthKey = await ConvertByMonthKeyAsync(billedMonths, firstMonthKey - 1, lastMonthKey, stoppingToken);

        return RevenueMovementCalculator.Calculate(
            revenueByMonthKey,
            FirstBilledMonthKeys(billedMonths),
            await EscalatedArrearsMonthKeysAsync(stoppingToken),
            RevenueConfig.ReportingCurrency,
            firstMonthKey,
            lastMonthKey);
    }

    public async Task<RevenueConcentration> GetConcentrationAsync(
        GetRevenueConcentrationRequest getRevenueConcentrationRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var monthKey = BillingPeriod.Key(getRevenueConcentrationRequest.Month, getRevenueConcentrationRequest.Year);

        var billedMonths = await invoiceRepository.GetBilledMonthsAsync(monthKey, monthKey, stoppingToken);
        var revenue = await ConvertByOrganizationAsync(billedMonths, stoppingToken);

        var earning = revenue
            .Where(x => x.Value.Revenue > 0)
            .OrderByDescending(x => x.Value.Revenue)
            .ToList();

        var total = earning.Sum(x => x.Value.Revenue);

        var customers = earning
            .Select((x, index) => new CustomerRevenueShare
            {
                Rank = new Rank(index + 1),
                OrganizationId = new OrganizationId(x.Key),
                OrganizationName = x.Value.Name,
                Revenue = new RevenueAmount(x.Value.Revenue),
                Share = new RevenueShare(total == 0 ? 0 : x.Value.Revenue / total * 100)
            })
            .ToList();

        return new RevenueConcentration
        {
            Month = getRevenueConcentrationRequest.Month,
            Year = getRevenueConcentrationRequest.Year,
            Currency = RevenueConfig.ReportingCurrency,
            TotalRevenue = new RevenueAmount(total),
            CustomerCount = new CustomerCount(customers.Count),
            LargestCustomerShare = TopShare(customers, 1),
            TopFiveShare = TopShare(customers, 5),
            TopTenShare = TopShare(customers, 10),

            ConcentrationIndex = customers.Count == 0
                ? null
                : new ConcentrationIndex(customers.Sum(x => x.Share / 100 * (x.Share / 100))),
            Customers = customers.Take(Math.Max(0, getRevenueConcentrationRequest.TopCount)).ToList()
        };
    }

    public async Task<List<CustomerMargin>> ListMarginsAsync(
        ListCustomerMarginRequest listCustomerMarginRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var month = listCustomerMarginRequest.Month;
        var year = listCustomerMarginRequest.Year;
        var monthKey = BillingPeriod.Key(month, year);

        var asOf = BillingPeriod.EndOf(month, year);

        var billedMonths = await invoiceRepository.GetBilledMonthsAsync(monthKey, monthKey, stoppingToken);
        var revenue = await ConvertByOrganizationAsync(billedMonths, stoppingToken);

        var usage = await usageRepository.GroupByOrganizationAndResourceAsync(
            new PeriodStart(BillingPeriod.StartOf(month, year)), new PeriodEnd(asOf), stoppingToken);

        var unitCosts = await ConvertUnitCostsAsync(asOf, stoppingToken);

        var usageByOrganization = usage
            .GroupBy(x => (Guid)x.OrganizationId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var organizationIds = revenue.Keys.Union(usageByOrganization.Keys).ToList();
        var names = await ResolveNamesAsync(organizationIds, revenue, stoppingToken);

        var margins = new List<CustomerMargin>();

        foreach (var organizationId in organizationIds)
        {
            var lines = (usageByOrganization.GetValueOrDefault(organizationId) ?? [])
                .ConvertAll(x => ToMarginLine(x, unitCosts))
                .OrderBy(x => x.BillableResource)
                .ToList();

            var earned = revenue.TryGetValue(organizationId, out var billed) ? billed.Revenue : 0m;
            var cost = lines.Sum(x => x.Cost);

            margins.Add(new CustomerMargin
            {
                OrganizationId = new OrganizationId(organizationId),
                OrganizationName = names[organizationId],
                Month = month,
                Year = year,
                Currency = RevenueConfig.ReportingCurrency,
                Revenue = new RevenueAmount(earned),
                Cost = new CostAmount(cost),
                Margin = new MarginAmount(earned - cost),

                MarginRate = earned == 0 ? null : new MarginRate((earned - cost) / earned * 100),
                CostsComplete = new CostsComplete(lines.TrueForAll(x => x.UnitCost.HasValue)),
                Lines = lines
            });
        }

        return margins.OrderBy(x => (decimal)x.Margin).ToList();
    }

    public async Task<RecoveryStatistic> GetRecoveryAsync(
        GetRecoveryStatisticRequest getRecoveryStatisticRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var cohort = await dunningRepository.ListOpenedBetweenAsync(
            getRecoveryStatisticRequest.PeriodStart, getRecoveryStatisticRequest.PeriodEnd, stoppingToken);

        var resolved = cohort.FindAll(x => x.ResolvedAt.HasValue);

        decimal recovered = 0;
        decimal outstanding = 0;

        foreach (var dunningState in cohort)
        {
            var invoice = dunningState.Invoice;

            if (invoice is null)
            {
                continue;
            }

            var amount = await ConvertAsync(
                invoice.NetAmount, invoice.Currency, BillingPeriod.EndOf(invoice.Month, invoice.Year), stoppingToken);

            if (dunningState.ResolvedAt.HasValue)
            {
                recovered += amount;
            }
            else
            {
                outstanding += amount;
            }
        }

        var involuntary = await GetInvoluntaryChurnAsync(
            getRecoveryStatisticRequest.PeriodStart, getRecoveryStatisticRequest.PeriodEnd, stoppingToken);

        return new RecoveryStatistic
        {
            PeriodStart = getRecoveryStatisticRequest.PeriodStart,
            PeriodEnd = getRecoveryStatisticRequest.PeriodEnd,
            Currency = RevenueConfig.ReportingCurrency,
            LaddersOpened = new Count(cohort.Count),
            LaddersResolved = new Count(resolved.Count),

            RecoveryRate = cohort.Count == 0
                ? null
                : new RecoveryRate((decimal)resolved.Count / cohort.Count * 100),
            RecoveredRevenue = new RevenueAmount(recovered),
            OutstandingRevenue = new RevenueAmount(outstanding),
            RestrictionsProposed = new Count(cohort.Count(x => x.RestrictionProposedAt.HasValue)),
            MeanDaysToRecover = resolved.Count == 0
                ? null
                : new DaysElapsed((int)Math.Round(resolved.Average(x =>
                    (x.ResolvedAt!.Value - (DateTimeOffset)x.CreatedAt).TotalDays))),
            ResolvedByStage = resolved
                .GroupBy(x => x.Stage)
                .OrderBy(x => x.Key)
                .Select(x => new StageRecovery { Stage = x.Key, Resolved = new Count(x.Count()) })
                .ToList(),
            InvoluntarilyChurnedCustomers = involuntary.Churned,
            InvoluntaryChurnRate = involuntary.Rate
        };
    }

    public async Task<List<CustomerHealth>> ListHealthAsync(
        ListCustomerHealthRequest listCustomerHealthRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var now = timeProvider.GetUtcNow();
        var windowDays = (int)listCustomerHealthRequest.WindowDays;

        var recentStart = new PeriodStart(now.AddDays(-windowDays));
        var previousStart = new PeriodStart(now.AddDays(-windowDays * 2));

        var recentUsage = await usageRepository.SumByOrganizationAsync(recentStart, new PeriodEnd(now), stoppingToken);
        var previousUsage = await usageRepository.SumByOrganizationAsync(previousStart, new PeriodEnd(recentStart), stoppingToken);

        var recentRequests = await SumRequestsAsync(recentStart, new PeriodEnd(now), stoppingToken);
        var previousRequests = await SumRequestsAsync(previousStart, new PeriodEnd(recentStart), stoppingToken);

        var lastMeteredAt = await usageRepository.GetLastMeteredAtAsync(stoppingToken);
        var arrears = await GetHighestOpenStagesAsync(stoppingToken);
        var organizations = await organizationRepository.GetAllAsync(stoppingToken);

        var health = organizations.ConvertAll(organization =>
        {
            var organizationId = (Guid)organization.OrganizationId;

            var recent = (int)recentUsage.GetValueOrDefault(organizationId);
            var previous = (int)previousUsage.GetValueOrDefault(organizationId);

            var usageTrend = previous == 0
                ? (TrendRate?)null
                : new TrendRate((decimal)(recent - previous) / previous * 100);

            var recentErrorRate = ToErrorRate(recentRequests.GetValueOrDefault(organizationId));
            var previousErrorRate = ToErrorRate(previousRequests.GetValueOrDefault(organizationId));

            var daysSinceLastUsage = lastMeteredAt.TryGetValue(organizationId, out var lastMetered)
                ? new DaysElapsed(Math.Max(0, (int)(now - lastMetered).TotalDays))
                : (DaysElapsed?)null;

            var arrearsStage = arrears.TryGetValue(organizationId, out var stage) ? stage : (DunningStage?)null;

            var (score, band, signals) = CustomerHealthScorer.Score(
                usageTrend, recentErrorRate, previousErrorRate, daysSinceLastUsage, arrearsStage, RevenueConfig);

            return new CustomerHealth
            {
                OrganizationId = organization.OrganizationId,
                OrganizationName = organization.FriendlyName,
                HealthScore = score,
                HealthBand = band,
                PeriodStart = recentStart,
                PeriodEnd = new PeriodEnd(now),
                RecentUsage = new Count(recent),
                PreviousUsage = new Count(previous),
                UsageTrend = usageTrend,
                RecentErrorRate = recentErrorRate,
                PreviousErrorRate = previousErrorRate,
                DaysSinceLastUsage = daysSinceLastUsage,
                ArrearsStage = arrearsStage,
                Signals = signals
            };
        });

        return health.OrderBy(x => (int)x.HealthScore).ToList();
    }

    public async Task<ExportedRevenueRecognition> ExportRecognitionAsync(
        ExportRevenueRecognitionRequest exportRevenueRecognitionRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var firstMonthKey = BillingPeriod.Key(exportRevenueRecognitionRequest.FromMonth, exportRevenueRecognitionRequest.FromYear);
        var lastMonthKey = BillingPeriod.Key(exportRevenueRecognitionRequest.ToMonth, exportRevenueRecognitionRequest.ToYear);

        var invoices = await invoiceRepository.ListIssuedByPeriodAsync(firstMonthKey, lastMonthKey, stoppingToken);
        var creditNotes = await creditNoteRepository.ListByInvoicePeriodAsync(firstMonthKey, lastMonthKey, stoppingToken);

        var billingEntityNames = new Dictionary<Guid, LegalName>();
        var entries = new List<RecognitionEntry>();

        foreach (var invoice in invoices)
        {
            var rate = await GetRateAsync(invoice.Currency, BillingPeriod.EndOf(invoice.Month, invoice.Year), stoppingToken);

            entries.Add(new RecognitionEntry
            {
                DocumentType = RecognitionDocumentType.Invoice,

                DocumentNumber = new DocumentNumber(invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString()),
                IssuedAt = invoice.IssuedAt!.Value,
                Month = invoice.Month,
                Year = invoice.Year,
                BillingEntityName = await ResolveBillingEntityNameAsync(invoice.BillingEntityId, billingEntityNames, stoppingToken),
                OrganizationId = invoice.OrganizationId,
                CustomerName = invoice.Customer?.LegalName,
                CustomerCountryCode = invoice.Customer?.CountryCode,
                Currency = invoice.Currency,
                NetAmount = invoice.NetAmount,
                TaxAmount = invoice.TaxAmount,
                Amount = invoice.Amount,
                TaxTreatment = invoice.TaxTreatment,
                TaxRate = invoice.TaxRate,
                ReportingCurrency = RevenueConfig.ReportingCurrency,
                ReportingRate = rate,
                ReportingNetAmount = new RevenueAmount(invoice.NetAmount * rate)
            });
        }

        foreach (var creditNote in creditNotes)
        {
            var invoice = creditNote.Invoice!;
            var rate = await GetRateAsync(creditNote.Currency, BillingPeriod.EndOf(invoice.Month, invoice.Year), stoppingToken);

            entries.Add(new RecognitionEntry
            {
                DocumentType = RecognitionDocumentType.CreditNote,
                DocumentNumber = new DocumentNumber(creditNote.CreditNoteNumber),
                IssuedAt = creditNote.IssuedAt,
                Month = invoice.Month,
                Year = invoice.Year,
                BillingEntityName = await ResolveBillingEntityNameAsync(creditNote.BillingEntityId, billingEntityNames, stoppingToken),
                OrganizationId = creditNote.OrganizationId,
                CustomerName = invoice.Customer?.LegalName,
                CustomerCountryCode = invoice.Customer?.CountryCode,
                Currency = creditNote.Currency,
                NetAmount = new NetAmount(-creditNote.NetAmount),
                TaxAmount = new TaxAmount(-creditNote.TaxAmount),
                Amount = new InvoiceAmount(-creditNote.Amount),
                TaxTreatment = creditNote.TaxTreatment,
                TaxRate = creditNote.TaxRate,
                ReportingCurrency = RevenueConfig.ReportingCurrency,
                ReportingRate = rate,
                ReportingNetAmount = new RevenueAmount(-(creditNote.NetAmount * rate))
            });
        }

        var ordered = entries
            .OrderBy(x => (int)x.Year)
            .ThenBy(x => (int)x.Month)
            .ThenBy(x => x.DocumentNumber.ToString(), StringComparer.Ordinal)
            .ToList();

        return new ExportedRevenueRecognition
        {
            Entries = ordered,
            Content = RevenueRecognitionCsvDocument.Write(ordered)
        };
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<Guid, decimal>>> ConvertByMonthKeyAsync(
        List<BilledMonth> billedMonths, int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken)
    {
        var revenueByMonthKey = new Dictionary<int, Dictionary<Guid, decimal>>();

        foreach (var billedMonth in billedMonths)
        {
            var monthKey = BillingPeriod.Key(billedMonth.Month, billedMonth.Year);

            if (monthKey < firstMonthKey || monthKey > lastMonthKey)
            {
                continue;
            }

            var amount = await ConvertAsync(
                billedMonth.NetAmount - billedMonth.CreditedNetAmount,
                billedMonth.Currency,
                BillingPeriod.EndOf(billedMonth.Month, billedMonth.Year),
                stoppingToken);

            var month = revenueByMonthKey.TryGetValue(monthKey, out var existing)
                ? existing
                : revenueByMonthKey[monthKey] = [];

            // Added rather than assigned: a customer billed in two currencies has two rows in one month.
            month[billedMonth.OrganizationId] = month.GetValueOrDefault(billedMonth.OrganizationId) + amount;
        }

        return revenueByMonthKey.ToDictionary(x => x.Key, x => (IReadOnlyDictionary<Guid, decimal>)x.Value);
    }

    private async Task<Dictionary<Guid, (FriendlyName Name, decimal Revenue)>> ConvertByOrganizationAsync(
        List<BilledMonth> billedMonths, CancellationToken stoppingToken)
    {
        var revenue = new Dictionary<Guid, (FriendlyName Name, decimal Revenue)>();

        foreach (var billedMonth in billedMonths)
        {
            var amount = await ConvertAsync(
                billedMonth.NetAmount - billedMonth.CreditedNetAmount,
                billedMonth.Currency,
                BillingPeriod.EndOf(billedMonth.Month, billedMonth.Year),
                stoppingToken);

            var organizationId = (Guid)billedMonth.OrganizationId;
            var running = revenue.TryGetValue(organizationId, out var existing) ? existing.Revenue : 0m;

            revenue[organizationId] = (billedMonth.OrganizationName, running + amount);
        }

        return revenue;
    }

    private static Dictionary<Guid, int> FirstBilledMonthKeys(List<BilledMonth> billedMonths)
    {
        var firstBilledMonthKey = new Dictionary<Guid, int>();

        foreach (var billedMonth in billedMonths)
        {
            var organizationId = (Guid)billedMonth.OrganizationId;
            var monthKey = BillingPeriod.Key(billedMonth.Month, billedMonth.Year);

            if (!firstBilledMonthKey.TryGetValue(organizationId, out var existing) || monthKey < existing)
            {
                firstBilledMonthKey[organizationId] = monthKey;
            }
        }

        return firstBilledMonthKey;
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlySet<int>>> EscalatedArrearsMonthKeysAsync(CancellationToken stoppingToken)
    {
        var openLadders = await dunningRepository.ListOpenAsync(stoppingToken);

        return openLadders
            .Where(x => x.Stage is DunningStage.FinalNotice or DunningStage.RestrictionProposed)
            .GroupBy(x => (Guid)x.OrganizationId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlySet<int>)x
                    .Select(ladder => BillingPeriod.Key(
                        new Month(((DateTimeOffset)ladder.CreatedAt).Month),
                        new Year(((DateTimeOffset)ladder.CreatedAt).Year)))
                    .ToHashSet());
    }

    private async Task<(Count Churned, ChurnRate? Rate)> GetInvoluntaryChurnAsync(
        PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        var months = await ListRecurringRevenueAsync(new ListRecurringRevenueRequest
        {
            FromMonth = new Month(from.Month),
            FromYear = new Year(from.Year),
            ToMonth = new Month(to.Month),
            ToYear = new Year(to.Year)
        }, stoppingToken);

        if (months.Count == 0)
        {
            return (new Count(0), null);
        }

        var churned = months.Sum(x => x.InvoluntarilyChurnedCustomers);
        var opening = (int)months[0].OpeningCustomers;

        return (new Count(churned), opening == 0 ? null : new ChurnRate(Math.Min(100m, (decimal)churned / opening * 100)));
    }

    private async Task<Dictionary<Guid, DunningStage>> GetHighestOpenStagesAsync(CancellationToken stoppingToken)
    {
        var openLadders = await dunningRepository.ListOpenAsync(stoppingToken);

        return openLadders
            .GroupBy(x => (Guid)x.OrganizationId)
            .ToDictionary(x => x.Key, x => x.Max(ladder => ladder.Stage));
    }

    private async Task<Dictionary<Guid, OrganizationRequestTotals>> SumRequestsAsync(
        PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var totals = await availabilityWindowRepository.SumByOrganizationAsync(periodStart, periodEnd, stoppingToken);

        return totals.ToDictionary(x => (Guid)x.OrganizationId, x => x);
    }

    private static ErrorRate ToErrorRate(OrganizationRequestTotals? totals)
    {
        if (totals is null || totals.TotalRequests == 0)
        {
            return new ErrorRate(0);
        }

        return new ErrorRate((decimal)(int)totals.FailedRequests / (int)totals.TotalRequests * 100);
    }

    private static CustomerMarginLine ToMarginLine(
        OrganizationResourceUsage usage, IReadOnlyDictionary<BillableResource, decimal> unitCosts)
    {
        var costed = unitCosts.TryGetValue(usage.BillableResource, out var unitCost);

        return new CustomerMarginLine
        {
            BillableResource = usage.BillableResource,
            Count = usage.Count,

            UnitCost = costed ? new UnitCost(unitCost) : null,
            Cost = new CostAmount(costed ? unitCost * (int)usage.Count : 0)
        };
    }

    private async Task<Dictionary<BillableResource, decimal>> ConvertUnitCostsAsync(
        DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var rates = await serviceCostRateService.GetInEffectAsync(asOf, stoppingToken);
        var unitCosts = new Dictionary<BillableResource, decimal>();

        foreach (var (billableResource, serviceCostRate) in rates)
        {
            unitCosts[billableResource] = await ConvertAsync(
                serviceCostRate.UnitCost, serviceCostRate.Currency, asOf, stoppingToken);
        }

        return unitCosts;
    }

    private async Task<LegalName?> ResolveBillingEntityNameAsync(
        BillingEntityId? billingEntityId, Dictionary<Guid, LegalName> cache, CancellationToken stoppingToken)
    {
        if (!billingEntityId.HasValue)
        {
            return null;
        }

        var id = (Guid)billingEntityId.Value;

        if (!cache.TryGetValue(id, out var legalName))
        {
            legalName = (await billingEntityService.GetAsync(billingEntityId.Value, stoppingToken)).LegalName;
            cache[id] = legalName;
        }

        return legalName;
    }

    private async Task<Dictionary<Guid, FriendlyName>> ResolveNamesAsync(
        List<Guid> organizationIds,
        Dictionary<Guid, (FriendlyName Name, decimal Revenue)> revenue,
        CancellationToken stoppingToken)
    {
        var names = revenue.ToDictionary(x => x.Key, x => x.Value.Name);

        var missing = organizationIds.FindAll(x => !names.ContainsKey(x));

        if (missing.Count == 0)
        {
            return names;
        }

        var organizations = await organizationRepository.ListByIdsAsync(
            missing.ConvertAll(x => new OrganizationId(x)), stoppingToken);

        foreach (var organization in organizations)
        {
            names[organization.OrganizationId] = organization.FriendlyName;
        }

        return names;
    }

    private static RevenueShare? TopShare(List<CustomerRevenueShare> customers, int take)
    {
        return customers.Count == 0
            ? null
            : new RevenueShare(Math.Min(100m, customers.Take(take).Sum(x => x.Share)));
    }

    private async Task<decimal> ConvertAsync(
        decimal amount, Currency currency, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return amount * await GetRateAsync(currency, asOf, stoppingToken);
    }

    private Task<ConversionRate> GetRateAsync(Currency currency, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return exchangeRateService.GetRateAsync(currency, RevenueConfig.ReportingCurrency, asOf, stoppingToken);
    }
}
