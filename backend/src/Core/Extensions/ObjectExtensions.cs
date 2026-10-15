using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Extensions;

public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(this object value)
    {
        return value switch
        {
            string s => s,
            _ => JsonSerializer.Serialize(value, Options)
        };
    }
}