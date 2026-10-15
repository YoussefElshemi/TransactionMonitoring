using Core.Enums;
using Core.Factories;
using Core.Models.Invoices;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Tax;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Factories;

public class InvoiceFactoryTests : BaseTestClass
{
    [Fact]
    public void Create_CreateInvoice()
    {
        // Arrange
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture);
        var invoiceId = FakeInvoiceId.CreateValid(Fixture);
        var planVersion = FakePlanVersion.CreateValid(Fixture);
        var billingProfile = FakeBillingProfile.CreateValid(Fixture);
        var lines = new List<InvoiceLine>
        {
            FakeInvoiceLine.CreateValid(Fixture) with { Amount = new LineAmount(30) },
            FakeInvoiceLine.CreateValid(Fixture) with { Amount = new LineAmount(12), SortOrder = new SortOrder(1) }
        };

        // Act
        var invoice = InvoiceFactory.Create(
            generateInvoiceRequest, invoiceId, planVersion, billingProfile, billingProfile.BillingEntity!,
            new ConversionRate(1m), FakeTaxDetermination.CreateValid(Fixture), lines,
            FakeDisplayPreferences.CreateValid().Locale, MockTimeProvider);

        // Assert
        invoice.InvoiceId.Should().Be(invoiceId);
        invoice.PlanVersionId.Should().Be(planVersion.PlanVersionId);
        invoice.CostPerRequest.Should().Be(planVersion.CostPerRequest);
        invoice.Lines.Should().BeEquivalentTo(lines);

        invoice.Amount.Should().Be(new InvoiceAmount(42));
        invoice.Year.Should().Be(generateInvoiceRequest.Year);
        invoice.Month.Should().Be(generateInvoiceRequest.Month);
        invoice.OrganizationId.Should().Be(generateInvoiceRequest.OrganizationId);
        invoice.Organization.Should().BeNull();
        invoice.CreatedByUserId.Should().Be(generateInvoiceRequest.CreatedByUserId);
        invoice.IsDeleted.Should().Be(new IsDeleted(false));
        invoice.DeletedAt.Should().BeNull();
        invoice.IsProcessed.Should().Be(new IsProcessed(false));
        invoice.ProcessedAt.Should().BeNull();

        invoice.InvoiceStatus.Should().Be(InvoiceStatus.Draft);
        invoice.InvoiceNumber.Should().BeNull();
        invoice.IssuedAt.Should().BeNull();
        invoice.DueAt.Should().BeNull();

        invoice.Supplier!.LegalName.Should().Be(billingProfile.BillingEntity!.LegalName);
        invoice.Customer!.LegalName.Should().Be(billingProfile.LegalName);
        invoice.Customer.CountryCode.Should().Be(billingProfile.CountryCode);
        invoice.PaymentTerms.Should().Be(billingProfile.PaymentTerms);

        invoice.Currency.Should().Be(billingProfile.Currency);
        invoice.PlanCurrency.Should().Be(planVersion.Currency);
        invoice.ConversionRate.Should().BeNull();
    }
}
