using AutoFixture;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class InvoiceLineMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_InvoiceLine()
    {
        // Arrange
        var invoiceLineEntity = FakeInvoiceLineEntity.CreateValid(Fixture);

        // Act
        var invoiceLine = invoiceLineEntity.ToModel();

        // Assert
        invoiceLine.InvoiceLineId.Should().Be(new InvoiceLineId(invoiceLineEntity.InvoiceLineId));
        invoiceLine.InvoiceId.Should().Be(new InvoiceId(invoiceLineEntity.InvoiceId));
        invoiceLine.InvoiceLineType.Should().Be((InvoiceLineType)invoiceLineEntity.InvoiceLineTypeId);
        invoiceLine.BillableResource.Should().Be((BillableResource)invoiceLineEntity.BillableResourceId!.Value);
        invoiceLine.Description.Should().Be(new InvoiceLineDescription(invoiceLineEntity.Description));
        invoiceLine.Quantity.Should().Be(new Quantity(invoiceLineEntity.Quantity));
        invoiceLine.UnitAmount.Should().Be(new UnitAmount(invoiceLineEntity.UnitAmount));
        invoiceLine.Amount.Should().Be(new LineAmount(invoiceLineEntity.Amount));
        invoiceLine.InvoiceAdjustmentId.Should().BeNull();
        invoiceLine.SortOrder.Should().Be(new SortOrder(invoiceLineEntity.SortOrder));
        invoiceLine.CreatedAt.Should().Be(new CreatedAt(invoiceLineEntity.CreatedAt));
    }

    [Fact]
    public void ToModel_ALineAboutNoParticularResource_KeepsItNull()
    {
        // Arrange
        var invoiceLineEntity = FakeInvoiceLineEntity.CreateValid(Fixture) with
        {
            InvoiceLineTypeId = (int)InvoiceLineType.PlatformFee,
            BillableResourceId = null
        };

        // Act
        var invoiceLine = invoiceLineEntity.ToModel();

        // Assert
        invoiceLine.BillableResource.Should().BeNull();
    }

    [Fact]
    public void ToModel_AnAdjustmentLine_CarriesTheOneOffItCameFrom()
    {
        // Arrange
        var invoiceAdjustmentId = Fixture.Create<Guid>();

        var invoiceLineEntity = FakeInvoiceLineEntity.CreateValid(Fixture) with
        {
            InvoiceLineTypeId = (int)InvoiceLineType.Adjustment,
            BillableResourceId = null,
            InvoiceAdjustmentId = invoiceAdjustmentId
        };

        // Act
        var invoiceLine = invoiceLineEntity.ToModel();

        // Assert
        invoiceLine.InvoiceAdjustmentId.Should().Be(new InvoiceAdjustmentId(invoiceAdjustmentId));
    }

    [Fact]
    public void ToModel_ACreditingLine_KeepsItsNegativeAmount()
    {
        // Arrange
        var invoiceLineEntity = FakeInvoiceLineEntity.CreateValid(Fixture) with
        {
            InvoiceLineTypeId = (int)InvoiceLineType.Discount,
            BillableResourceId = null,
            Amount = -42.75m
        };

        // Act
        var invoiceLine = invoiceLineEntity.ToModel();

        // Assert
        invoiceLine.Amount.Should().Be(new LineAmount(-42.75m));
    }

    [Fact]
    public void ToEntity_InvoiceLine()
    {
        // Arrange
        var invoiceLine = FakeInvoiceLine.CreateValid(Fixture);

        // Act
        var invoiceLineEntity = invoiceLine.ToEntity();

        // Assert
        invoiceLineEntity.InvoiceLineId.Should().Be(invoiceLine.InvoiceLineId);
        invoiceLineEntity.InvoiceId.Should().Be(invoiceLine.InvoiceId);
        invoiceLineEntity.InvoiceLineTypeId.Should().Be((int)invoiceLine.InvoiceLineType);
        invoiceLineEntity.BillableResourceId.Should().Be((int)invoiceLine.BillableResource!.Value);
        invoiceLineEntity.Description.Should().Be(invoiceLine.Description);
        invoiceLineEntity.Quantity.Should().Be(invoiceLine.Quantity);
        invoiceLineEntity.UnitAmount.Should().Be(invoiceLine.UnitAmount);
        invoiceLineEntity.Amount.Should().Be(invoiceLine.Amount);
        invoiceLineEntity.InvoiceAdjustmentId.Should().BeNull();
        invoiceLineEntity.SortOrder.Should().Be(invoiceLine.SortOrder);
        invoiceLineEntity.CreatedAt.Should().Be(invoiceLine.CreatedAt);
    }

    [Fact]
    public void ToEntity_AnAdjustmentLine_CarriesTheOneOffItCameFrom()
    {
        // Arrange
        var invoiceAdjustmentId = FakeInvoiceAdjustmentId.CreateValid(Fixture);

        var invoiceLine = FakeInvoiceLine.CreateValid(Fixture) with
        {
            InvoiceLineType = InvoiceLineType.Adjustment,
            BillableResource = null,
            InvoiceAdjustmentId = invoiceAdjustmentId
        };

        // Act
        var invoiceLineEntity = invoiceLine.ToEntity();

        // Assert
        invoiceLineEntity.BillableResourceId.Should().BeNull();
        invoiceLineEntity.InvoiceAdjustmentId.Should().Be((Guid)invoiceAdjustmentId);
    }
}
