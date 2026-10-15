using Core.Enums;
using Core.Models.Billing;
using Core.Models.Invoices;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class InvoiceMapper
{
    public static Invoice ToModel(this InvoiceEntity invoiceEntity)
    {
        return new Invoice
        {
            InvoiceId = new InvoiceId(invoiceEntity.InvoiceId),
            InvoiceNumber = invoiceEntity.InvoiceNumber is null ? null : new InvoiceNumber(invoiceEntity.InvoiceNumber),
            InvoiceStatus = (InvoiceStatus)invoiceEntity.InvoiceStatusId,
            BillingEntityId = invoiceEntity.BillingEntityId.HasValue ? new BillingEntityId(invoiceEntity.BillingEntityId.Value) : null,
            PlanVersionId = invoiceEntity.PlanVersionId.HasValue ? new PlanVersionId(invoiceEntity.PlanVersionId.Value) : null,
            PlanVersion = invoiceEntity.PlanVersion?.ToModel(),
            CostPerRequest = new CostPerRequest(invoiceEntity.CostPerRequest),
            Supplier = ToParty(
                invoiceEntity.SupplierName, invoiceEntity.SupplierAddress,
                invoiceEntity.SupplierCountryCode, invoiceEntity.SupplierTaxIdentifier, null),
            Customer = ToParty(
                invoiceEntity.CustomerName, invoiceEntity.CustomerAddress,
                invoiceEntity.CustomerCountryCode, invoiceEntity.CustomerTaxIdentifier, invoiceEntity.CustomerEmail),
            Currency = (Currency)invoiceEntity.CurrencyId,
            PlanCurrency = (Currency)invoiceEntity.PlanCurrencyId,
            ConversionRate = invoiceEntity.ConversionRate.HasValue ? new ConversionRate(invoiceEntity.ConversionRate.Value) : null,
            TaxTreatment = (TaxTreatment)invoiceEntity.TaxTreatmentId,
            TaxRate = new TaxRate(invoiceEntity.TaxRate),
            TaxAmount = new TaxAmount(invoiceEntity.TaxAmount),
            NetAmount = new NetAmount(invoiceEntity.NetAmount),
            Amount = new InvoiceAmount(invoiceEntity.Amount),
            Month = new Month(invoiceEntity.Month),
            Year = new Year(invoiceEntity.Year),
            IssuedAt = invoiceEntity.IssuedAt.HasValue ? new IssuedAt(invoiceEntity.IssuedAt.Value) : null,
            DueAt = invoiceEntity.DueAt.HasValue ? new DueAt(invoiceEntity.DueAt.Value) : null,
            PaymentTerms = new PaymentTerms(invoiceEntity.PaymentTerms),
            OrganizationId = new OrganizationId(invoiceEntity.OrganizationId),
            Organization = invoiceEntity.Organization.ToModel(),
            CreatedByUserId = invoiceEntity.CreatedByUserId.HasValue ? new ApplicationUserId(invoiceEntity.CreatedByUserId.Value) : null,
            Locale = invoiceEntity.Locale is not null ? new Locale(invoiceEntity.Locale) : null,
            Lines = invoiceEntity.Lines
                .OrderBy(x => x.SortOrder)
                .Select(x => x.ToModel())
                .ToList(),
            CreatedAt = new CreatedAt(invoiceEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(invoiceEntity.UpdatedAt),
            IsProcessed = new IsProcessed(invoiceEntity.IsProcessed),
            ProcessedAt = invoiceEntity.ProcessedAt.HasValue ? new ProcessedAt(invoiceEntity.ProcessedAt.Value) : null,
            IsDeleted = new IsDeleted(invoiceEntity.IsDeleted),
            DeletedAt = invoiceEntity.DeletedAt.HasValue ? new DeletedAt(invoiceEntity.DeletedAt.Value) : null
        };
    }

    public static InvoiceEntity ToEntity(this Invoice invoice)
    {
        return new InvoiceEntity
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber.HasValue ? invoice.InvoiceNumber.Value : null,
            InvoiceStatusId = (int)invoice.InvoiceStatus,
            BillingEntityId = invoice.BillingEntityId.HasValue ? invoice.BillingEntityId.Value : null,
            PlanVersionId = invoice.PlanVersionId,
            CostPerRequest = invoice.CostPerRequest,
            SupplierName = invoice.Supplier?.LegalName,
            SupplierAddress = invoice.Supplier?.Address,
            SupplierCountryCode = invoice.Supplier?.CountryCode,
            SupplierTaxIdentifier = invoice.Supplier?.TaxIdentifier.HasValue == true ? invoice.Supplier.TaxIdentifier.Value : null,
            CustomerName = invoice.Customer?.LegalName,
            CustomerAddress = invoice.Customer?.Address,
            CustomerCountryCode = invoice.Customer?.CountryCode,
            CustomerTaxIdentifier = invoice.Customer?.TaxIdentifier.HasValue == true ? invoice.Customer.TaxIdentifier.Value : null,
            CustomerEmail = invoice.Customer?.Email.HasValue == true ? invoice.Customer.Email.Value : null,
            CurrencyId = (int)invoice.Currency,
            PlanCurrencyId = (int)invoice.PlanCurrency,
            ConversionRate = invoice.ConversionRate.HasValue ? invoice.ConversionRate.Value : null,
            TaxTreatmentId = (int)invoice.TaxTreatment,
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            NetAmount = invoice.NetAmount,
            Amount = invoice.Amount,
            Year = invoice.Year,
            Month = invoice.Month,
            IssuedAt = invoice.IssuedAt.HasValue ? invoice.IssuedAt.Value : null,
            DueAt = invoice.DueAt.HasValue ? invoice.DueAt.Value : null,
            PaymentTerms = invoice.PaymentTerms,
            OrganizationId = invoice.OrganizationId,
            CreatedByUserId = invoice.CreatedByUserId,
            Locale = invoice.Locale?.ToString(),
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            IsProcessed = invoice.IsProcessed,
            ProcessedAt = invoice.ProcessedAt,
            IsDeleted = invoice.IsDeleted,
            DeletedAt = invoice.DeletedAt
        };
    }

    private static DocumentParty? ToParty(string? legalName, string? address, string? countryCode, string? taxIdentifier, string? email)
    {
        if (legalName is null || address is null || countryCode is null)
        {
            return null;
        }

        return new DocumentParty
        {
            LegalName = new LegalName(legalName),
            Address = new PostalAddress(address),
            CountryCode = new CountryCode(countryCode),
            TaxIdentifier = taxIdentifier is null ? null : new TaxIdentifier(taxIdentifier),
            Email = email is null ? null : new Email(email)
        };
    }
}
