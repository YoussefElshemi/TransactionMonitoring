using System.Net.Http.Json;
using Presentation.Models.Common;

namespace BehaviouralTests.TestHelpers;

public static class HttpContentExtensions
{
    public static async Task<T?> ReadDataFromJsonAsync<T>(this HttpContent content)
    {
        var responseData = await content.ReadFromJsonAsync<ResponseDataDto<T>>();

        return responseData is null ? default : responseData.Data;
    }
}
