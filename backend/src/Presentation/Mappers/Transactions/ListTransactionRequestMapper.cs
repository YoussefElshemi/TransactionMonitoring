using Core.Models.Transactions;
using Core.ValueObjects;
using Presentation.Models.Transactions;

namespace Presentation.Mappers.Transactions;

public static class ListTransactionRequestMapper
{
    public static ListTransactionRequest ToModel(this ListTransactionRequestDto listTransactionRequestDto)
    {
        return new ListTransactionRequest
        {
            OrganizationId = new OrganizationId(listTransactionRequestDto.OrganizationId),
            FriendlyName = listTransactionRequestDto.FriendlyName != null ? new FriendlyName(listTransactionRequestDto.FriendlyName) : null,
            ExternalReference = listTransactionRequestDto.ExternalReference != null ? new ExternalReference(listTransactionRequestDto.ExternalReference) : null,
            PageNumber = new PageNumber(listTransactionRequestDto.PageNumber),
            PageSize = new PageSize(listTransactionRequestDto.PageSize),
            FromDate = new DateRange(listTransactionRequestDto.FromDate),
            ToDate = new DateRange(listTransactionRequestDto.ToDate)
        };
    }
}
