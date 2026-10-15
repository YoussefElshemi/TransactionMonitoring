using Presentation.Models.Common;

namespace Presentation.Mappers.Common;

public static class ResponseDataMapper
{
    public static ResponseDataDto<T> ToResponseData<T>(this T data)
    {
        return new ResponseDataDto<T>
        {
            Data = data
        };
    }
}
