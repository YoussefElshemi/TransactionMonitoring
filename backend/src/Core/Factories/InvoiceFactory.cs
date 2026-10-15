using Core.Enums;
using Core.Helpers;
using Core.Models.Billing;
using Core.Models.Invoices;
using Core.Models.Plans;
using Core.Models.Tax;
using Core.ValueObjects;

namespace Core.Factories;

public static class InvoiceFactory
{
    public static Invoice Create(
        GenerateInvoiceRequest generateInvoiceRequest,
        InvoiceId invoiceId,
        PlanVersion planVersion,
        BillingProfile billingProfile,
        BillingEntity billingEntity,
        ConversionRate conversionRate,
        TaxDetermination taxDetermination,
        List<InvoiceLine> lines,
        Locale locale,
        TimeProvider timeProvider)
    {
        var amount = PricingCalculator.Total(lines);

        return new Invoice
        {
            InvoiceId = invoiceId,

            InvoiceNumber = null,
            InvoiceStatus = InvoiceStatus.Draft,
            IssuedAt = null,
            DueAt = null,

            BillingEntityId = billingEntity.BillingEntityId,
            PlanVersionId = planVersion.PlanVersionId,
            PlanVersion = null,
            CostPerRequest = planVersion.CostPerRequest,

            Supplier = new DocumentParty
            {
                LegalName = billingEntity.LegalName,
                Address = billingEntity.Address,
                CountryCode = billingEntity.CountryCode,
                TaxIdentifier = billingEntity.TaxIdentifier,
                Email = null
            },
            Customer = new DocumentParty
            {
                LegalName = billingProfile.LegalName,
                Address = billingProfile.Address,
                CountryCode = billingProfile.CountryCode,

                TaxIdentifier = billingProfile.TaxIdentifierVerifiedAt.HasValue ? billingProfile.TaxIdentifier : null,
                Email = billingProfile.ContactEmail
            },

            Currency = billingProfile.Currency,
            PlanCurrency = planVersion.Currency,
            ConversionRate = billingProfile.Currency == planVersion.Currency ? null : conversionRate,

            TaxTreatment = taxDetermination.TaxTreatment,
            TaxRate = taxDetermination.TaxRate,
            TaxAmount = PricingCalculator.Tax(lines),
            NetAmount = PricingCalculator.Net(lines),
            Amount = amount,

            PaymentTerms = billingProfile.PaymentTerms,
            Year = generateInvoiceRequest.Year,
            Month = generateInvoiceRequest.Month,
            OrganizationId = generateInvoiceRequest.OrganizationId,
            Organization = null,
            CreatedByUserId = generateInvoiceRequest.CreatedByUserId,
            Lines = lines,

            Locale = locale,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null,

            IsProcessed = new IsProcessed(amount <= 0),
            ProcessedAt = amount <= 0 ? new ProcessedAt(timeProvider.GetUtcNow()) : null
        };
    }
}
