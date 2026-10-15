using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;

namespace UnitTests.Tests.Core.Mappers;

public class CreateInvoiceAdjustmentRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateInvoiceAdjustmentRequest()
    {
        // Arrange
        var request = FakeCreateInvoiceAdjustmentRequest.CreateValid(Fixture);
        var now = MockTimeProvider.GetUtcNow();

        // Act
        var invoiceAdjustment = request.ToModel(MockTimeProvider);

        // Assert
        invoiceAdjustment.InvoiceAdjustmentId.Should().NotBe(new InvoiceAdjustmentId(Guid.Empty));
        invoiceAdjustment.OrganizationId.Should().Be(request.OrganizationId);
        invoiceAdjustment.Month.Should().Be(request.Month);
        invoiceAdjustment.Year.Should().Be(request.Year);
        invoiceAdjustment.Description.Should().Be(request.Description);
        invoiceAdjustment.Amount.Should().Be(request.Amount);
        invoiceAdjustment.CreatedByUserId.Should().Be(request.CreatedByUserId);
        invoiceAdjustment.CreatedAt.Should().Be(new CreatedAt(now));
        invoiceAdjustment.UpdatedAt.Should().Be(new UpdatedAt(now));
        invoiceAdjustment.IsDeleted.Should().Be(new IsDeleted(false));
        invoiceAdjustment.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToModel_RaisesItUnapplied()
    {
        // Arrange
        var request = FakeCreateInvoiceAdjustmentRequest.CreateValid(Fixture);

        // Act
        var invoiceAdjustment = request.ToModel(MockTimeProvider);

        // Assert
        invoiceAdjustment.AppliedToInvoiceId.Should().BeNull();
    }

    [Theory]
    [InlineData(-500.00)]
    [InlineData(750.25)]
    public void ToModel_KeepsTheSignOfTheAmount(decimal amount)
    {
        // Arrange
        var request = FakeCreateInvoiceAdjustmentRequest.CreateValid(Fixture) with
        {
            Amount = new AdjustmentAmount(amount)
        };

        // Act
        var invoiceAdjustment = request.ToModel(MockTimeProvider);

        // Assert
        invoiceAdjustment.Amount.Should().Be(new AdjustmentAmount(amount));
    }
}
