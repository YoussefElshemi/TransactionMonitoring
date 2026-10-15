using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class InvoiceAdjustmentMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_InvoiceAdjustment()
    {
        // Arrange
        var invoiceAdjustmentEntity = FakeInvoiceAdjustmentEntity.CreateValid(Fixture);

        // Act
        var invoiceAdjustment = invoiceAdjustmentEntity.ToModel();

        // Assert
        invoiceAdjustment.InvoiceAdjustmentId.Should().Be(new InvoiceAdjustmentId(invoiceAdjustmentEntity.InvoiceAdjustmentId));
        invoiceAdjustment.OrganizationId.Should().Be(new OrganizationId(invoiceAdjustmentEntity.OrganizationId));
        invoiceAdjustment.Month.Should().Be(new Month(invoiceAdjustmentEntity.Month));
        invoiceAdjustment.Year.Should().Be(new Year(invoiceAdjustmentEntity.Year));
        invoiceAdjustment.Description.Should().Be(new InvoiceAdjustmentDescription(invoiceAdjustmentEntity.Description));
        invoiceAdjustment.AppliedToInvoiceId.Should().Be(new InvoiceId(invoiceAdjustmentEntity.AppliedToInvoiceId!.Value));
        invoiceAdjustment.CreatedByUserId.Should().Be(new ApplicationUserId(invoiceAdjustmentEntity.CreatedByUserId!.Value));
        invoiceAdjustment.CreatedAt.Should().Be(new CreatedAt(invoiceAdjustmentEntity.CreatedAt));
        invoiceAdjustment.UpdatedAt.Should().Be(new UpdatedAt(invoiceAdjustmentEntity.UpdatedAt));
        invoiceAdjustment.IsDeleted.Should().Be(new IsDeleted(invoiceAdjustmentEntity.IsDeleted));
        invoiceAdjustment.DeletedAt.Should().Be(new DeletedAt(invoiceAdjustmentEntity.DeletedAt!.Value));
    }

    [Theory]
    [InlineData(-250.50)]
    [InlineData(1200.00)]
    public void ToModel_KeepsTheSignOfTheAmount(decimal amount)
    {
        // Arrange
        var invoiceAdjustmentEntity = FakeInvoiceAdjustmentEntity.CreateValid(Fixture) with { Amount = amount };

        // Act
        var invoiceAdjustment = invoiceAdjustmentEntity.ToModel();

        // Assert
        invoiceAdjustment.Amount.Should().Be(new AdjustmentAmount(amount));
    }

    [Fact]
    public void ToModel_UnappliedAdjustment_KeepsTheInvoiceNull()
    {
        // Arrange
        var invoiceAdjustmentEntity = FakeInvoiceAdjustmentEntity.CreateValid(Fixture) with { AppliedToInvoiceId = null };

        // Act
        var invoiceAdjustment = invoiceAdjustmentEntity.ToModel();

        // Assert
        invoiceAdjustment.AppliedToInvoiceId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_InvoiceAdjustment()
    {
        // Arrange
        var invoiceAdjustment = FakeInvoiceAdjustment.CreateValid(Fixture);

        // Act
        var invoiceAdjustmentEntity = invoiceAdjustment.ToEntity();

        // Assert
        invoiceAdjustmentEntity.InvoiceAdjustmentId.Should().Be(invoiceAdjustment.InvoiceAdjustmentId);
        invoiceAdjustmentEntity.OrganizationId.Should().Be(invoiceAdjustment.OrganizationId);
        invoiceAdjustmentEntity.Month.Should().Be(invoiceAdjustment.Month);
        invoiceAdjustmentEntity.Year.Should().Be(invoiceAdjustment.Year);
        invoiceAdjustmentEntity.Description.Should().Be(invoiceAdjustment.Description);
        invoiceAdjustmentEntity.Amount.Should().Be(invoiceAdjustment.Amount);
        invoiceAdjustmentEntity.CreatedByUserId.Should().Be(invoiceAdjustment.CreatedByUserId!.Value);
        invoiceAdjustmentEntity.CreatedAt.Should().Be(invoiceAdjustment.CreatedAt);
        invoiceAdjustmentEntity.UpdatedAt.Should().Be(invoiceAdjustment.UpdatedAt);
        invoiceAdjustmentEntity.IsDeleted.Should().Be(invoiceAdjustment.IsDeleted);
    }
}
