using AutoFixture;
using Core.Enums;
using Core.Models.Billing;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;

public static class FakeBillingEntity
{
    public static BillingEntity CreateValid(IFixture fixture)
    {
        return new BillingEntity
        {
            BillingEntityId = FakeBillingEntityId.CreateValid(fixture),
            LegalName = FakeLegalName.CreateValid(fixture),
            Address = FakePostalAddress.CreateValid(fixture),
            CountryCode = FakeCountryCode.CreateValid(fixture),
            TaxIdentifier = FakeTaxIdentifier.CreateValid(fixture),
            InvoiceNumberPrefix = new NumberPrefix("INV"),
            CreditNoteNumberPrefix = new NumberPrefix("CRN"),
            Currency = Currency.Gbp,
            IsEnabled = new IsEnabled(true),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
