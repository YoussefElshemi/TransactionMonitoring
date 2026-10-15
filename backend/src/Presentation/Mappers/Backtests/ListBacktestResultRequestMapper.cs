using Core.Models.BackTests;
using Core.ValueObjects;
using Presentation.Models.BackTests;

namespace Presentation.Mappers.BackTests;

public static class ListBackTestResultRequestMapper
{
    public static ListBackTestResultRequest ToModel(this ListBackTestResultRequestDto dto)
    {
        return new ListBackTestResultRequest
        {
            BackTestId = new BackTestId(dto.BackTestId),
            ChangedOnly = dto.ChangedOnly,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
