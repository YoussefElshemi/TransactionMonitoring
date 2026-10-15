using Core.Models.Users;
using Core.ValueObjects;
using Presentation.Models.Users;

namespace Presentation.Mappers.Users;

public static class ListUserRequestMapper
{
    public static ListUserRequest ToModel(this ListUserRequestDto listRequestDto)
    {
        return new ListUserRequest
        {
            UserName = !string.IsNullOrWhiteSpace(listRequestDto.UserName) ? new UserName(listRequestDto.UserName) : null,
            PageNumber = new PageNumber(listRequestDto.PageNumber),
            PageSize = new PageSize(listRequestDto.PageSize),
            FromDate = new DateRange(listRequestDto.FromDate),
            ToDate = new DateRange(listRequestDto.ToDate)
        };
    }
}