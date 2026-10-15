using AutoFixture;
using Core.Enums;
using Core.Models.Billing;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;

public static class FakeBillingProfile
{
    public static BillingProfile CreateValid(IFixture fixture)
    {
        var billingEntity = FakeBillingEntity.CreateValid(fixture);

        return new BillingProfile
        {
            BillingProfileId = FakeBillingProfileId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            BillingEntityId = billingEntity.BillingEntityId,
            BillingEntity = billingEntity,
            LegalName = FakeLegalName.CreateValid(fixture),
            ContactName = FakeContactName.CreateValid(fixture),
            ContactEmail = FakeEmail.CreateValid(fixture),
            Address = FakePostalAddress.CreateValid(fixture),
            CountryCode = FakeCountryCode.CreateValid(fixture),
            TaxIdentifier = null,
            TaxIdentifierVerifiedAt = null,
            ProviderCustomerReference = null,
            Currency = Currency.Gbp,
            PaymentTerms = new PaymentTerms(28),
            UpdatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            Locale = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
