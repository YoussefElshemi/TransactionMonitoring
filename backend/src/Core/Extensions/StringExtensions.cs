using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Extensions;

public static class StringExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public static string ToBase64String(this string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    public static string FromBase64String(this string value)
    {
        var bytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(bytes);
    }

    public static T TryDeserialize<T>(this string value)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value, Options)
                   ?? throw new InvalidOperationException(nameof(value));
        }
        catch
        {
            throw new InvalidOperationException(nameof(value));
        }
    }
}