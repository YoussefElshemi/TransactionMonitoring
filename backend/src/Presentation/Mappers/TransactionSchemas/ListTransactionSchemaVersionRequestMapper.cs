using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Mappers.TransactionSchemas;

public static class ListTransactionSchemaVersionRequestMapper
{
    public static ListTransactionSchemaVersionRequest ToModel(this ListTransactionSchemaVersionRequestDto listTransactionSchemaVersionRequestDto)
    {
        return new ListTransactionSchemaVersionRequest
        {
            OrganizationId = new OrganizationId(listTransactionSchemaVersionRequestDto.OrganizationId),
            PageNumber = new PageNumber(listTransactionSchemaVersionRequestDto.PageNumber),
            PageSize = new PageSize(listTransactionSchemaVersionRequestDto.PageSize),
            FromDate = new DateRange(listTransactionSchemaVersionRequestDto.FromDate),
            ToDate = new DateRange(listTransactionSchemaVersionRequestDto.ToDate)
        };
    }

    public static ListTransactionSchemaVersionRequest ToModel(this ListMyTransactionSchemaVersionRequestDto listMyTransactionSchemaVersionRequestDto)
    {
        return new ListTransactionSchemaVersionRequest
        {
            OrganizationId = new OrganizationId(listMyTransactionSchemaVersionRequestDto.OrganizationId),
            PageNumber = new PageNumber(listMyTransactionSchemaVersionRequestDto.PageNumber),
            PageSize = new PageSize(listMyTransactionSchemaVersionRequestDto.PageSize),
            FromDate = new DateRange(listMyTransactionSchemaVersionRequestDto.FromDate),
            ToDate = new DateRange(listMyTransactionSchemaVersionRequestDto.ToDate)
        };
    }
}
