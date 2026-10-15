using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class DocumentNumberFormatterTests : BaseTestClass
{
    [Fact]
    public void Invoice_GivenAPositionInAYear_ComposesPrefixYearAndPosition()
    {
        // Arrange
        // Act
        var invoiceNumber = DocumentNumberFormatter.Invoice(new NumberPrefix("ACME"), new Year(2026), 42);

        // Assert
        ((string)invoiceNumber).Should().Be("ACME-2026-000042");
    }

    [Fact]
    public void Invoice_GivenPositionsEitherSideOfAPowerOfTen_KeepsThemInIssueOrderWhenSortedAsText()
    {
        // Arrange
        var prefix = new NumberPrefix("ACME");
        var year = new Year(2026);

        // Act
        var ninth = DocumentNumberFormatter.Invoice(prefix, year, 9);
        var tenth = DocumentNumberFormatter.Invoice(prefix, year, 10);

        // Assert
        string.CompareOrdinal(ninth, tenth).Should().BeNegative();
    }

    [Fact]
    public void CreditNote_GivenTheSamePositionAsAnInvoice_IsDistinguishableByItsPrefix()
    {
        // Arrange
        var year = new Year(2026);

        // Act
        var invoiceNumber = DocumentNumberFormatter.Invoice(new NumberPrefix("INV"), year, 1);
        var creditNoteNumber = DocumentNumberFormatter.CreditNote(new NumberPrefix("CRN"), year, 1);

        // Assert
        ((string)invoiceNumber).Should().Be("INV-2026-000001");
        ((string)creditNoteNumber).Should().Be("CRN-2026-000001");
    }

    [Fact]
    public void Invoice_GivenAPositionBelowOne_Throws()
    {
        // Arrange
        // Act
        var act = () => DocumentNumberFormatter.Invoice(new NumberPrefix("INV"), new Year(2026), 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
