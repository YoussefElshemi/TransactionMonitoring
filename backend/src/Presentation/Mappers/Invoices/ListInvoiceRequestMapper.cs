using Core.Models.Invoices;
using Core.ValueObjects;
using Presentation.Models.Invoices;

namespace Presentation.Mappers.Invoices;

public static class ListInvoiceRequestMapper
{
    public static ListInvoiceRequest ToModel(this ListInvoiceRequestDto listInvoiceRequestDto)
    {
        return new ListInvoiceRequest
        {
            OrganizationId = new OrganizationId(listInvoiceRequestDto.OrganizationId),
            PageNumber = new PageNumber(listInvoiceRequestDto.PageNumber),
            PageSize = new PageSize(listInvoiceRequestDto.PageSize),
            FromDate = new DateRange(listInvoiceRequestDto.FromDate),
            ToDate = new DateRange(listInvoiceRequestDto.ToDate)
        };
    }
}