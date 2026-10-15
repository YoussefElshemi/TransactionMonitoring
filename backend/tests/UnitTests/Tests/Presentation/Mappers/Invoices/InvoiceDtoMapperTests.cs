using FluentAssertions;
using Presentation.Mappers.Invoices;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;

namespace UnitTests.Tests.Presentation.Mappers.Invoices;

public class InvoiceDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Invoice()
    {
        // Arrange
        var @case = FakeInvoice.CreateValid(Fixture);

        // Act
        var caseDto = @case.ToDto();

        caseDto.InvoiceId.Should().Be(@case.InvoiceId);
        caseDto.Amount.Should().Be(@case.Amount);
        caseDto.Month.Should().Be(@case.Month);
        caseDto.Year.Should().Be(@case.Year);
        caseDto.CreatedAt.Should().Be(@case.CreatedAt);
        caseDto.UpdatedAt.Should().Be(@case.UpdatedAt);
        caseDto.IsProcessed.Should().Be(@case.IsProcessed);
        caseDto.ProcessedAt.Should().Be(@case.ProcessedAt);
    }
}