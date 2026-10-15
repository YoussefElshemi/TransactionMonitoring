using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class InvoiceMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Invoice()
    {
        // Arrange
        var planVersionEntity = FakePlanVersionEntity.CreateValid(Fixture);
        var invoiceEntity = FakeInvoiceEntity.CreateValid(Fixture) with
        {
            PlanVersionId = planVersionEntity.PlanVersionId,
            PlanVersion = planVersionEntity
        };

        // Act
        var invoice = invoiceEntity.ToModel();

        // Assert
        invoice.InvoiceId.Should().Be(new InvoiceId(invoiceEntity.InvoiceId));
        invoice.Month.Should().Be(new Month(invoiceEntity.Month));
        invoice.Year.Should().Be(new Year(invoiceEntity.Year));
        invoice.PlanVersionId.Should().Be(new PlanVersionId(invoiceEntity.PlanVersionId!.Value));
        invoice.PlanVersion.Should().BeEquivalentTo(invoiceEntity.PlanVersion!.ToModel());
        invoice.CostPerRequest.Should().Be(new CostPerRequest(invoiceEntity.CostPerRequest));
        invoice.Amount.Should().Be(new InvoiceAmount(invoiceEntity.Amount));
        invoice.IsProcessed.Should().Be(new IsProcessed(invoiceEntity.IsProcessed));
        invoice.ProcessedAt.Should().Be(new ProcessedAt(invoiceEntity.ProcessedAt!.Value));
        invoice.CreatedByUserId.Should().Be(new ApplicationUserId(invoiceEntity.CreatedByUserId!.Value));
        invoice.OrganizationId.Should().Be(new OrganizationId(invoiceEntity.OrganizationId));
        invoice.Organization.Should().BeEquivalentTo(invoiceEntity.Organization.ToModel());
        invoice.CreatedAt.Should().Be(new CreatedAt(invoiceEntity.CreatedAt));
        invoice.UpdatedAt.Should().Be(new UpdatedAt(invoiceEntity.UpdatedAt));
        invoice.IsDeleted.Should().Be(new IsDeleted(invoiceEntity.IsDeleted));
        invoice.DeletedAt.Should().Be(new DeletedAt(invoiceEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_Invoice()
    {
        // Arrange
        var invoice = FakeInvoice.CreateValid(Fixture);

        // Act
        var invoiceEntity = invoice.ToEntity();

        // Assert
        invoiceEntity.InvoiceId.Should().Be(invoice.InvoiceId);
        invoiceEntity.Month.Should().Be(invoice.Month);
        invoiceEntity.Year.Should().Be(invoice.Year);
        invoiceEntity.Amount.Should().Be(invoice.Amount);
        invoiceEntity.PlanVersionId.Should().Be(invoice.PlanVersionId!.Value);
        invoiceEntity.CostPerRequest.Should().Be(invoice.CostPerRequest);
        invoiceEntity.IsProcessed.Should().Be(invoice.IsProcessed);
        invoiceEntity.ProcessedAt.Should().Be(invoice.ProcessedAt);
        invoiceEntity.OrganizationId.Should().Be(invoice.OrganizationId);
        invoiceEntity.CreatedAt.Should().Be(invoice.CreatedAt);
        invoiceEntity.UpdatedAt.Should().Be(invoice.UpdatedAt);
        invoiceEntity.IsDeleted.Should().Be(invoice.IsDeleted);
        invoiceEntity.DeletedAt.Should().Be(invoice.DeletedAt!.Value);
    }
}