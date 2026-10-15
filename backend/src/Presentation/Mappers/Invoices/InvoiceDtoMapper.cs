using Core.Helpers;
using Core.Models.Billing;
using Core.Models.Invoices;
using Presentation.Models.Invoices;

namespace Presentation.Mappers.Invoices;

public static class InvoiceDtoMapper
{
    public static InvoiceResponseDto ToDto(this Invoice invoice)
    {
        return new InvoiceResponseDto
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber.HasValue ? invoice.InvoiceNumber.Value : null,
            InvoiceStatus = invoice.InvoiceStatus.ToString(),
            BillingEntityId = invoice.BillingEntityId.HasValue ? invoice.BillingEntityId.Value : null,
            PlanVersionId = invoice.PlanVersionId.HasValue ? invoice.PlanVersionId.Value : null,
            PlanVersionNumber = invoice.PlanVersion is not null ? invoice.PlanVersion.VersionNumber : null,
            CostPerRequest = invoice.CostPerRequest,
            Supplier = invoice.Supplier.ToDto(),
            Customer = invoice.Customer.ToDto(),
            Currency = CurrencyFormatter.Code(invoice.Currency),
            PlanCurrency = CurrencyFormatter.Code(invoice.PlanCurrency),
            ConversionRate = invoice.ConversionRate.HasValue ? invoice.ConversionRate.Value : null,
            TaxTreatment = invoice.TaxTreatment.ToString(),
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            NetAmount = invoice.NetAmount,
            Amount = invoice.Amount,
            IssuedAt = invoice.IssuedAt.HasValue ? invoice.IssuedAt.Value : null,
            DueAt = invoice.DueAt.HasValue ? invoice.DueAt.Value : null,
            PaymentTerms = invoice.PaymentTerms,
            OrganizationId = invoice.OrganizationId,
            Month = invoice.Month,
            Year = invoice.Year,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            IsProcessed = invoice.IsProcessed,
            ProcessedAt = invoice.ProcessedAt,
            Lines = invoice.Lines.ConvertAll(x => x.ToDto())
        };
    }

    public static InvoicePartyResponseDto? ToDto(this DocumentParty? documentParty)
    {
        if (documentParty is null)
        {
            return null;
        }

        return new InvoicePartyResponseDto
        {
            LegalName = documentParty.LegalName,
            Address = documentParty.Address,
            CountryCode = documentParty.CountryCode,
            TaxIdentifier = documentParty.TaxIdentifier.HasValue ? documentParty.TaxIdentifier.Value : null,
            Email = documentParty.Email.HasValue ? documentParty.Email.Value : null
        };
    }

    public static InvoiceLineResponseDto ToDto(this InvoiceLine invoiceLine)
    {
        return new InvoiceLineResponseDto
        {
            InvoiceLineId = invoiceLine.InvoiceLineId,
            InvoiceLineType = invoiceLine.InvoiceLineType.ToString(),
            BillableResource = invoiceLine.BillableResource?.ToString(),
            Description = invoiceLine.Description,
            Quantity = invoiceLine.Quantity,
            UnitAmount = invoiceLine.UnitAmount,
            Amount = invoiceLine.Amount,
            InvoiceAdjustmentId = invoiceLine.InvoiceAdjustmentId.HasValue ? invoiceLine.InvoiceAdjustmentId.Value : null,
            SortOrder = invoiceLine.SortOrder
        };
    }
}
