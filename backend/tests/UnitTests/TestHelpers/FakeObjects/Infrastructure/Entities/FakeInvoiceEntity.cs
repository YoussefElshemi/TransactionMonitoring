using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeInvoiceEntity
{
    public static InvoiceEntity CreateValid(IFixture fixture)
    {
        return new InvoiceEntity
        {
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            InvoiceId = fixture.Create<Guid>(),

            // No billing entity or parties: invented ids would violate foreign keys when behavioural tests insert this.
            InvoiceNumber = null,
            InvoiceStatusId = (int)InvoiceStatus.Draft,
            BillingEntityId = null,
            SupplierName = null,
            SupplierAddress = null,
            SupplierCountryCode = null,
            SupplierTaxIdentifier = null,
            CustomerName = null,
            CustomerAddress = null,
            CustomerCountryCode = null,
            CustomerTaxIdentifier = null,
            CustomerEmail = null,
            CurrencyId = (int)Currency.Gbp,
            PlanCurrencyId = (int)Currency.Gbp,
            ConversionRate = null,
            TaxTreatmentId = (int)TaxTreatment.OutOfScope,
            TaxRate = 0m,
            TaxAmount = 0m,
            IssuedAt = null,
            DueAt = null,
            PaymentTerms = 28,
            Month = fixture.Create<int>(),
            Year = fixture.Create<int>(),
            // Null: an invented plan version id would violate the foreign key.
            PlanVersionId = null,
            PlanVersion = null,
            CostPerRequest = fixture.Create<decimal>(),
            NetAmount = fixture.Create<decimal>(),
            Amount = fixture.Create<decimal>(),
            IsProcessed = fixture.Create<bool>(),
            ProcessedAt = fixture.Create<DateTimeOffset>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture),
            Locale = null
        };
    }
}