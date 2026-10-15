using Core.Constants;
using Core.Enums;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BehaviouralTests.TestHelpers;

public static class DatabaseHelper
{
    public static async Task CreateOrganizations(IServiceProvider serviceProvider, IEnumerable<OrganizationEntity> organizationEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Organizations.AddRangeAsync(organizationEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateApiClients(IServiceProvider serviceProvider, IEnumerable<ApiClientEntity> apiClientEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.ApiClients.AddRangeAsync(apiClientEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateCases(IServiceProvider serviceProvider, IEnumerable<CaseEntity> caseEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Cases.AddRangeAsync(caseEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateCaseReasonCodes(IServiceProvider serviceProvider, IEnumerable<CaseReasonCodeEntity> caseReasonCodeEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.CaseReasonCodes.AddRangeAsync(caseReasonCodeEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateTransactions(IServiceProvider serviceProvider, IEnumerable<TransactionEntity> transactionEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Transactions.AddRangeAsync(transactionEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateDecisions(IServiceProvider serviceProvider, IEnumerable<DecisionEntity> decisionEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Decisions.AddRangeAsync(decisionEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateCaseNotes(IServiceProvider serviceProvider, IEnumerable<CaseNoteEntity> caseNoteEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.CaseNotes.AddRangeAsync(caseNoteEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateInvoices(IServiceProvider serviceProvider, IEnumerable<InvoiceEntity> invoiceEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Invoices.AddRangeAsync(invoiceEntities);
        await context.SaveChangesAsync();
    }

    public static async Task<PlanVersionEntity> CreatePlanWithSubscription(
        IServiceProvider serviceProvider,
        Guid organizationId,
        decimal costPerRequest,
        DateTimeOffset effectiveFrom)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var planEntity = new PlanEntity
        {
            PlanId = Guid.NewGuid(),
            Name = $"Plan {organizationId}",
            Description = null,
            OrganizationId = organizationId,
            IsEnabled = true,
            CurrentVersionNumber = 1,
            CreatedAt = effectiveFrom,
            UpdatedAt = effectiveFrom,
            IsDeleted = false,
            DeletedAt = null
        };

        var planVersionEntity = new PlanVersionEntity
        {
            PlanVersionId = Guid.NewGuid(),
            PlanId = planEntity.PlanId,
            VersionNumber = 1,
            CurrencyId = (int)Currency.Gbp,
            CostPerRequest = costPerRequest,
            PlatformFee = 0,
            MinimumCharge = 0,
            PublishedByUserId = null,
            CreatedAt = effectiveFrom
        };

        // Pricing reads only price rows, so a version without the catch-all bills nothing.
        var planPriceEntity = new PlanPriceEntity
        {
            PlanPriceId = Guid.NewGuid(),
            PlanVersionId = planVersionEntity.PlanVersionId,
            BillableResourceId = null,
            PricingModelId = (int)PricingModel.Flat,
            FreeAllowance = 0,
            CreatedAt = effectiveFrom
        };

        var planPriceTierEntity = new PlanPriceTierEntity
        {
            PlanPriceTierId = Guid.NewGuid(),
            PlanPriceId = planPriceEntity.PlanPriceId,
            FromQuantity = 0,
            ToQuantity = null,
            UnitAmount = costPerRequest,
            FlatAmount = 0,
            CreatedAt = effectiveFrom
        };

        var subscriptionEntity = new SubscriptionEntity
        {
            SubscriptionId = Guid.NewGuid(),
            OrganizationId = organizationId,
            PlanVersionId = planVersionEntity.PlanVersionId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            CreatedByUserId = null,
            CreatedAt = effectiveFrom,
            UpdatedAt = effectiveFrom,
            IsDeleted = false,
            DeletedAt = null
        };

        await context.Plans.AddAsync(planEntity);
        await context.PlanVersions.AddAsync(planVersionEntity);
        await context.PlanPrices.AddAsync(planPriceEntity);
        await context.PlanPriceTiers.AddAsync(planPriceTierEntity);
        await context.Subscriptions.AddAsync(subscriptionEntity);
        await context.SaveChangesAsync();

        return planVersionEntity;
    }

    // The endpoint refuses to backdate, so move the row directly.
    public static async Task BackdateContractDiscount(
        IServiceProvider serviceProvider,
        Guid contractDiscountId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset effectiveTo)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var contractDiscountEntity = await context.ContractDiscounts
            .FirstAsync(x => x.ContractDiscountId == contractDiscountId);

        contractDiscountEntity.EffectiveFrom = effectiveFrom;
        contractDiscountEntity.EffectiveTo = effectiveTo;

        context.ContractDiscounts.Update(contractDiscountEntity);

        await context.SaveChangesAsync();
    }

    public static async Task BackdateSubscriptions(IServiceProvider serviceProvider, Guid organizationId, DateTimeOffset effectiveFrom)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var subscriptionEntities = await context.Subscriptions
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync();

        foreach (var subscriptionEntity in subscriptionEntities)
        {
            subscriptionEntity.EffectiveFrom = effectiveFrom;
            context.Subscriptions.Update(subscriptionEntity);
        }

        await context.SaveChangesAsync();
    }

    public static async Task ShiftSubscriptions(IServiceProvider serviceProvider, Guid organizationId, TimeSpan by)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var subscriptionEntities = await context.Subscriptions
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync();

        foreach (var subscriptionEntity in subscriptionEntities)
        {
            subscriptionEntity.EffectiveFrom = subscriptionEntity.EffectiveFrom.Add(by);
            subscriptionEntity.EffectiveTo = subscriptionEntity.EffectiveTo?.Add(by);

            context.Subscriptions.Update(subscriptionEntity);
        }

        await context.SaveChangesAsync();
    }

    public static async Task<Guid> CreateBillingProfile(
        IServiceProvider serviceProvider,
        Guid organizationId,
        string supplierCountryCode = "GB",
        string customerCountryCode = "GB",
        Currency currency = Currency.Gbp,
        string? customerTaxIdentifier = null,
        bool taxIdentifierVerified = false,
        string invoiceNumberPrefix = "INV",
        string creditNoteNumberPrefix = "CRN")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var now = DateTimeOffset.UtcNow;

        var billingEntityEntity = new BillingEntityEntity
        {
            BillingEntityId = Guid.NewGuid(),
            LegalName = "Transaction Monitoring Ltd",
            Address = "1 Example Street\nLondon\nEC1A 1AA",
            CountryCode = supplierCountryCode,
            TaxIdentifier = supplierCountryCode == "GB" ? "GB123456789" : null,
            InvoiceNumberPrefix = invoiceNumberPrefix,
            CreditNoteNumberPrefix = creditNoteNumberPrefix,
            CurrencyId = (int)Currency.Gbp,
            IsEnabled = true,
            CreatedByUserId = null,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };

        var billingProfileEntity = new BillingProfileEntity
        {
            BillingProfileId = Guid.NewGuid(),
            OrganizationId = organizationId,
            BillingEntityId = billingEntityEntity.BillingEntityId,
            LegalName = "Customer Ltd",
            ContactName = "Accounts Payable",
            ContactEmail = "accounts@customer.test",
            Address = "2 Example Street\nManchester\nM1 1AA",
            CountryCode = customerCountryCode,
            TaxIdentifier = customerTaxIdentifier,
            TaxIdentifierVerifiedAt = taxIdentifierVerified ? now : null,
            ProviderCustomerReference = null,
            CurrencyId = (int)currency,
            PaymentTerms = 28,
            UpdatedByUserId = null,
            Locale = null,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };

        await context.BillingEntities.AddAsync(billingEntityEntity);
        await context.BillingProfiles.AddAsync(billingProfileEntity);
        await context.SaveChangesAsync();

        return billingEntityEntity.BillingEntityId;
    }

    public static async Task CreateExchangeRate(
        IServiceProvider serviceProvider,
        Currency fromCurrency,
        Currency toCurrency,
        decimal rate,
        DateTimeOffset effectiveFrom)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var now = DateTimeOffset.UtcNow;

        await context.ExchangeRates.AddAsync(new ExchangeRateEntity
        {
            ExchangeRateId = Guid.NewGuid(),
            FromCurrencyId = (int)fromCurrency,
            ToCurrencyId = (int)toCurrency,
            Rate = rate,
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = null,
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();
    }

    public static async Task CreateUsages(IServiceProvider serviceProvider, IEnumerable<UsageEntity> usageEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Usages.AddRangeAsync(usageEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateAvailabilityWindow(
        IServiceProvider serviceProvider,
        Guid organizationId,
        DateTimeOffset periodStart,
        int totalRequests,
        int failedRequests,
        int latencyMilliseconds = 40)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var now = DateTimeOffset.UtcNow;
        var availabilityWindowId = Guid.NewGuid();
        var latencyBucketWidth = AvailabilityConstants.LatencyBucketWidthMilliseconds;
        var latencyBucketStart = (long)latencyMilliseconds / latencyBucketWidth * latencyBucketWidth;
        var latencyBucketUpperBound = (int)Math.Min(latencyBucketStart + latencyBucketWidth - 1, int.MaxValue);

        await context.AvailabilityWindows.AddAsync(new AvailabilityWindowEntity
        {
            AvailabilityWindowId = availabilityWindowId,
            OrganizationId = organizationId,
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMinutes(AvailabilityConstants.WindowMinutes),
            TotalRequests = totalRequests,
            FailedRequests = failedRequests,
            TotalLatencyMilliseconds = (long)totalRequests * latencyMilliseconds,
            MaxLatencyMilliseconds = latencyMilliseconds,
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.AvailabilityWindowLatencyBuckets.AddAsync(new AvailabilityWindowLatencyBucketEntity
        {
            AvailabilityWindowId = availabilityWindowId,
            UpperBoundMilliseconds = latencyBucketUpperBound,
            Count = totalRequests,
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();
    }

    public static async Task CreateApplicationUsers(IServiceProvider serviceProvider, IEnumerable<ApplicationUserEntity> applicationUserEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Users.AddRangeAsync(applicationUserEntities);
        await context.SaveChangesAsync();
    }

    public static async Task SetRuleAuthor(IServiceProvider serviceProvider, Guid ruleId, Guid userId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // CreatedByUserId is init-only on the entity, so this reassigns it in SQL.
        await context.Rules
            .Where(x => x.RuleId == ruleId)
            .ExecuteUpdateAsync(x => x.SetProperty(rule => rule.CreatedByUserId, userId));
    }

    public static async Task MoveUserToOrganization(IServiceProvider serviceProvider, Guid userId, Guid organizationId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // The context queries with NoTrackingWithIdentityResolution, so a read-modify-SaveChanges here would silently persist nothing.
        await context.Users
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(x => x.SetProperty(user => user.OrganizationId, organizationId));
    }

    public static async Task CreateApiClientSecrets(IServiceProvider serviceProvider, IEnumerable<ApiClientSecretEntity> apiClientSecretEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.ApiClientSecrets.AddRangeAsync(apiClientSecretEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateWebhookConfigurations(IServiceProvider serviceProvider, IEnumerable<WebhookConfigurationEntity> webhookConfigurationEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.WebhookConfigurations.AddRangeAsync(webhookConfigurationEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateRuleSets(IServiceProvider serviceProvider, IEnumerable<RuleSetEntity> ruleSetEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.RuleSets.AddRangeAsync(ruleSetEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateOutboxMessages(IServiceProvider serviceProvider, IEnumerable<OutboxMessageEntity> outboxMessageEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.OutboxMessages.AddRangeAsync(outboxMessageEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateBackTests(IServiceProvider serviceProvider, IEnumerable<BackTestEntity> backTestEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.BackTests.AddRangeAsync(backTestEntities);
        await context.SaveChangesAsync();
    }

    public static async Task CreateAuditLogs(IServiceProvider serviceProvider, IEnumerable<AuditLogEntity> auditLogEntities)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.AuditLogs.AddRangeAsync(auditLogEntities);
        await context.SaveChangesAsync();
    }
}
