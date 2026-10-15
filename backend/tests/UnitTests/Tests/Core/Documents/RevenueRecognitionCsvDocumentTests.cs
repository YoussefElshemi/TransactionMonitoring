using System.Text;
using Core.Documents;
using Core.Enums;
using Core.Models.Revenue;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Documents;

public class RevenueRecognitionCsvDocumentTests : BaseTestClass
{
    [Fact]
    public void Write_LeadsWithAHeaderRowAndOneRowPerDocument()
    {
        // Arrange
        var entries = new List<RecognitionEntry> { Invoice("INV-0001"), Invoice("INV-0002") };

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write(entries));

        // Assert
        lines.Should().HaveCount(3);
        lines[0].Should().StartWith("DocumentType,DocumentNumber,IssuedAt,RecognitionPeriod");
        lines[1].Should().Contain("INV-0001");
        lines[2].Should().Contain("INV-0002");
    }

    [Fact]
    public void Write_CarriesTheServiceMonthRatherThanTheMonthTheDocumentWasIssuedIn()
    {
        // Arrange
        var entry = Invoice("INV-0001") with
        {
            Month = new Month(12),
            Year = new Year(2025),
            IssuedAt = new IssuedAt(new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero))
        };

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write([entry]));

        // Assert
        lines[1].Should().Contain("\"2025-12\"");
        lines[1].Should().Contain("2026-01-02T09:00:00");
    }

    [Fact]
    public void Write_CarriesACreditNoteAsANegativeRowInThePeriodItReverses()
    {
        // Arrange
        var entry = CreditNote("CRN-0001");

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write([entry]));

        // Assert
        lines[1].Should().StartWith("\"CreditNote\"");
        lines[1].Should().Contain("\"-250\"");
    }

    [Fact]
    public void Write_WritesAmountsUnformattedSoTheyImportAsNumbers()
    {
        // Arrange
        var entry = Invoice("INV-0001") with { NetAmount = new NetAmount(1234567.89m) };

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write([entry]));

        // Assert
        lines[1].Should().Contain("\"1234567.89\"");
        lines[1].Should().NotContain("1,234,567");
        lines[1].Should().NotContain("£");
    }

    [Fact]
    public void Write_CarriesTheReportingRateBesideTheConvertedAmount()
    {
        // Arrange
        var entry = Invoice("INV-0001") with
        {
            Currency = Currency.Eur,
            NetAmount = new NetAmount(100m),
            ReportingRate = new ConversionRate(0.86m),
            ReportingNetAmount = new RevenueAmount(86m)
        };

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write([entry]));

        // Assert
        lines[1].Should().Contain("\"EUR\"").And.Contain("\"GBP\"");
        lines[1].Should().Contain("\"0.86\"").And.Contain("\"86\"");
    }

    [Fact]
    public void Write_QuotesACustomerNameContainingACommaWithoutShiftingTheColumns()
    {
        // Arrange
        var entry = Invoice("INV-0001") with { CustomerName = new LegalName("Borden, Fell and Co") };

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write([entry]));

        // Assert
        lines[1].Should().Contain("\"Borden, Fell and Co\"");
        lines[1].Split("\",\"").Should().HaveCount(17);
    }

    [Fact]
    public void Write_LeavesTheFieldsEmptyOnDocumentsThatNeverCarriedParties()
    {
        // Arrange
        var entry = Invoice("INV-0001") with
        {
            BillingEntityName = null,
            CustomerName = null,
            CustomerCountryCode = null
        };

        // Act
        var lines = Read(RevenueRecognitionCsvDocument.Write([entry]));

        // Assert
        lines[1].Should().Contain(",\"\",");
        lines[1].Should().NotContain("null");
    }

    [Fact]
    public void Write_LeadsWithAByteOrderMark()
    {
        // Act
        var content = RevenueRecognitionCsvDocument.Write([Invoice("INV-0001")]);

        // Assert
        content.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
    }

    private static List<string> Read(byte[] content)
    {
        return Encoding.UTF8.GetString(content)
            .TrimStart('﻿')
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static RecognitionEntry Invoice(string documentNumber)
    {
        return new RecognitionEntry
        {
            DocumentType = RecognitionDocumentType.Invoice,
            DocumentNumber = new DocumentNumber(documentNumber),
            IssuedAt = new IssuedAt(new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero)),
            Month = new Month(3),
            Year = new Year(2026),
            BillingEntityName = new LegalName("Transaction Monitoring Ltd"),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            CustomerName = new LegalName("Acme Payments"),
            CustomerCountryCode = new CountryCode("GB"),
            Currency = Currency.Gbp,
            NetAmount = new NetAmount(1000m),
            TaxAmount = new TaxAmount(200m),
            Amount = new InvoiceAmount(1200m),
            TaxTreatment = TaxTreatment.Standard,
            TaxRate = new TaxRate(20m),
            ReportingCurrency = Currency.Gbp,
            ReportingRate = new ConversionRate(1m),
            ReportingNetAmount = new RevenueAmount(1000m)
        };
    }

    private static RecognitionEntry CreditNote(string documentNumber)
    {
        return Invoice(documentNumber) with
        {
            DocumentType = RecognitionDocumentType.CreditNote,
            DocumentNumber = new DocumentNumber(documentNumber),
            NetAmount = new NetAmount(-250m),
            TaxAmount = new TaxAmount(-50m),
            Amount = new InvoiceAmount(-300m),
            ReportingNetAmount = new RevenueAmount(-250m)
        };
    }
}
