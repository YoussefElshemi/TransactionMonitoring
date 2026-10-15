using Core.Configs;
using Core.Documents;
using Core.Enums;
using Core.Exceptions;
using Core.Factories;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Billing;
using Core.Models.Common;
using Core.Models.Invoices;
using Core.Models.Plans;
using Core.Models.Usages;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace Core.Services;

public class InvoiceService(
    IInvoiceRepository invoiceRepository,
    IInvoiceAdjustmentRepository invoiceAdjustmentRepository,
    IDocumentNumberRepository documentNumberRepository,
    IUsageService usageService,
    ISubscriptionService subscriptionService,
    IContractDiscountService contractDiscountService,
    ICommitmentService commitmentService,
    IBillingProfileService billingProfileService,
    IBillingEntityService billingEntityService,
    IExchangeRateService exchangeRateService,
    IDisplayPreferenceService displayPreferenceService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext,
    IOptions<AppConfig> appConfig) : IInvoiceService
{
    public async Task<Invoice> GenerateAsync(GenerateInvoiceRequest generateInvoiceRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var asOf = BillingPeriod.EndOf(generateInvoiceRequest.Month, generateInvoiceRequest.Year);

        var planVersion = await ResolvePlanVersionAsync(generateInvoiceRequest, asOf, stoppingToken);
        var billingProfile = await ResolveBillingProfileAsync(generateInvoiceRequest.OrganizationId, stoppingToken);
        var billingEntity = await billingEntityService.GetAsync(billingProfile.BillingEntityId, stoppingToken);

        var conversionRate = await exchangeRateService.GetRateAsync(
            planVersion.Currency, billingProfile.Currency, asOf, stoppingToken);

        var translator = new Translator(await displayPreferenceService.ForStatedAsync(
            billingProfile.Locale, generateInvoiceRequest.OrganizationId, stoppingToken));

        var taxDetermination = TaxDeterminer.Determine(
            billingEntity.CountryCode,
            billingProfile.CountryCode,
            billingProfile.TaxIdentifier,
            billingProfile.TaxIdentifierVerifiedAt,
            translator);

        var invoiceId = new InvoiceId(Guid.NewGuid());

        var priceInvoiceRequest = new PriceInvoiceRequest
        {
            InvoiceId = invoiceId,
            PlanVersion = planVersion,
            Quantities = await usageService.GroupByResourceAsync(
                new SumUsageByDateRequest
                {
                    OrganizationId = generateInvoiceRequest.OrganizationId,
                    Month = generateInvoiceRequest.Month,
                    Year = generateInvoiceRequest.Year
                },
                stoppingToken),
            Discounts = await contractDiscountService.GetInEffectAsync(generateInvoiceRequest.OrganizationId, asOf, stoppingToken),
            Adjustments = await invoiceAdjustmentRepository.GetUnappliedAsync(
                generateInvoiceRequest.OrganizationId, generateInvoiceRequest.Month, generateInvoiceRequest.Year, stoppingToken),
            Commitment = await commitmentService.GetInEffectAsync(generateInvoiceRequest.OrganizationId, asOf, stoppingToken),
            Currency = billingProfile.Currency,
            ConversionRate = conversionRate,
            TaxDetermination = taxDetermination,
            Translator = translator
        };

        var lines = PricingCalculator.Calculate(priceInvoiceRequest, timeProvider);

        var invoice = InvoiceFactory.Create(
            generateInvoiceRequest, invoiceId, planVersion, billingProfile, billingEntity,
            conversionRate, taxDetermination, lines, translator.DisplayPreferences.Locale, timeProvider);

        await invoiceRepository.AddAsync(invoice, stoppingToken);

        // Stamped in the same unit of work, or next month's run would pick the adjustment up again.
        foreach (var adjustment in priceInvoiceRequest.Adjustments)
        {
            await invoiceAdjustmentRepository.ApplyAsync(adjustment.InvoiceAdjustmentId, invoiceId, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(invoice.InvoiceId, stoppingToken);
    }

    public async Task<Invoice> IssueAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var invoice = await GetAsync(invoiceId, stoppingToken);

            if (invoice.InvoiceStatus is not InvoiceStatus.Draft || invoice.BillingEntityId is null)
            {
                throw new InvoiceIssuedException(invoice.InvoiceId, invoice.InvoiceNumber);
            }

            var billingEntity = await billingEntityService.GetAsync(invoice.BillingEntityId.Value, stoppingToken);
            var issuedAt = timeProvider.GetUtcNow();

            var issuedYear = new Year(issuedAt.Year);

            var position = await documentNumberRepository.NextInvoicePositionAsync(
                billingEntity.BillingEntityId, issuedYear, stoppingToken);

            await invoiceRepository.IssueAsync(
                invoiceId,
                DocumentNumberFormatter.Invoice(billingEntity.InvoiceNumberPrefix, issuedYear, position),
                new IssuedAt(issuedAt),
                new DueAt(issuedAt.AddDays(invoice.PaymentTerms)),
                stoppingToken);

            await unitOfWork.SaveChangesAsync(stoppingToken);

            return await GetAsync(invoiceId, stoppingToken);
        }, stoppingToken);
    }

    private async Task<PlanVersion> ResolvePlanVersionAsync(
        GenerateInvoiceRequest generateInvoiceRequest,
        DateTimeOffset asOf,
        CancellationToken stoppingToken)
    {
        var subscription = await subscriptionService.GetInEffectAsync(
            generateInvoiceRequest.OrganizationId, asOf, stoppingToken);

        if (subscription?.PlanVersion is null)
        {
            throw new NoPlanInEffectException(
                generateInvoiceRequest.OrganizationId, generateInvoiceRequest.Month, generateInvoiceRequest.Year);
        }

        return subscription.PlanVersion;
    }

    private async Task<BillingProfile> ResolveBillingProfileAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return await billingProfileService.FindAsync(organizationId, stoppingToken)
               ?? throw new NoBillingProfileException(organizationId);
    }

    public async Task<ExportedInvoice> ExportAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var invoice = await GetAsync(invoiceId, stoppingToken);

        var document = new InvoiceDocument(
            invoice, appConfig.Value.InvoiceConfig, TranslatorFor(invoice.Locale));

        return new ExportedInvoice
        {
            Invoice = invoice,
            Content = document.GeneratePdf()
        };
    }

    private Translator TranslatorFor(Locale? locale)
    {
        return new Translator(displayPreferenceService.Resolve(locale, null, null));
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceId invoiceId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return invoiceRepository.ExistsAsync(organizationId, invoiceId, includeDeleted, stoppingToken);
    }

    public Task<Exists> ExistsByDateAsync(OrganizationId organizationId, Month month, Year year, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return invoiceRepository.ExistsByDateAsync(organizationId, month, year, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Invoice>> ListAsync(ListInvoiceRequest listInvoiceRequest, CancellationToken stoppingToken)
    {
        return invoiceRepository.ListAsync(listInvoiceRequest, stoppingToken);
    }

    public Task<Invoice> GetAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        return invoiceRepository.GetAsync(invoiceId, stoppingToken);
    }

    public Task<Invoice?> GetIssuedByDateAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        return invoiceRepository.GetIssuedByDateAsync(organizationId, month, year, stoppingToken);
    }

    public Task<List<Invoice>> GetOverdueAsync(CancellationToken stoppingToken)
    {
        return invoiceRepository.GetOverdueAsync(stoppingToken);
    }

    public async Task ProcessAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoice = await GetAsync(invoiceId, stoppingToken);

        if (invoice.InvoiceStatus is not InvoiceStatus.Issued)
        {
            throw new InvoiceNotIssuedException(invoiceId);
        }

        await invoiceRepository.ProcessAsync(invoiceId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoice = await GetAsync(invoiceId, stoppingToken);

        if (invoice.InvoiceStatus is not InvoiceStatus.Draft)
        {
            throw new InvoiceIssuedException(invoice.InvoiceId, invoice.InvoiceNumber);
        }

        await invoiceRepository.DeleteAsync(invoiceId, stoppingToken);
        await invoiceAdjustmentRepository.ReleaseAsync(invoiceId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        await invoiceRepository.RecoverAsync(invoiceId, stoppingToken);
        await invoiceAdjustmentRepository.ReapplyAsync(invoiceId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListInvoiceRequest listInvoiceRequest, CancellationToken stoppingToken)
    {
        return invoiceRepository.GetCountAsync(listInvoiceRequest, stoppingToken);
    }
}
