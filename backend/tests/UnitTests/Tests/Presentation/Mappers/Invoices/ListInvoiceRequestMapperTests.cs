using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Invoices;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Invoices;

namespace UnitTests.Tests.Presentation.Mappers.Invoices;

public class ListInvoiceRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListInvoiceRequest()
    {
        // Arrange
        var listInvoiceRequestDto = FakeListInvoiceRequestDto.CreateValid(Fixture);

        // Act
        var listInvoiceRequest = listInvoiceRequestDto.ToModel();

        // Assert
        listInvoiceRequest.OrganizationId.Should().Be(new OrganizationId(listInvoiceRequestDto.OrganizationId));
        listInvoiceRequest.PageNumber.Should().Be(new PageNumber(listInvoiceRequestDto.PageNumber));
        listInvoiceRequest.PageSize.Should().Be(new PageSize(listInvoiceRequestDto.PageSize));
        listInvoiceRequest.FromDate.Should().Be(new DateRange(listInvoiceRequestDto.FromDate));
        listInvoiceRequest.ToDate.Should().Be(new DateRange(listInvoiceRequestDto.ToDate));
    }
}