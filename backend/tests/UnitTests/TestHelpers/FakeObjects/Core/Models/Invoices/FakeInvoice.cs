using AutoFixture;
using Core.Enums;
using Core.Models.Invoices;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;

public static class FakeInvoice
{
    public static Invoice CreateValid(IFixture fixture)
    {
        return new Invoice
        {
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            InvoiceId = FakeInvoiceId.CreateValid(fixture),
            InvoiceNumber = FakeInvoiceNumber.CreateValid(fixture),
            InvoiceStatus = InvoiceStatus.Issued,
            BillingEntityId = FakeBillingEntityId.CreateValid(fixture),
            Supplier = FakeDocumentParty.CreateValid(fixture),
            Customer = FakeDocumentParty.CreateValid(fixture),
            Currency = Currency.Gbp,
            PlanCurrency = Currency.Gbp,
            ConversionRate = null,
            TaxTreatment = TaxTreatment.OutOfScope,
            TaxRate = new TaxRate(0m),
            TaxAmount = new TaxAmount(0m),
            IssuedAt = FakeIssuedAt.CreateValid(fixture),
            DueAt = FakeDueAt.CreateValid(fixture),
            PaymentTerms = new PaymentTerms(28),
            Month = FakeMonth.CreateValid(fixture),
            Year = FakeYear.CreateValid(fixture),
            PlanVersionId = FakePlanVersionId.CreateValid(fixture),
            PlanVersion = FakePlanVersion.CreateValid(fixture),
            CostPerRequest = FakeCostPerRequest.CreateValid(fixture),
            NetAmount = FakeNetAmount.CreateValid(fixture),
            Amount = FakeInvoiceAmount.CreateValid(fixture),
            IsProcessed = FakeIsProcessed.CreateValid(fixture),
            ProcessedAt = FakeProcessedAt.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Organization = FakeOrganization.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            Lines = [],
            Locale = null,
        };
    }
}