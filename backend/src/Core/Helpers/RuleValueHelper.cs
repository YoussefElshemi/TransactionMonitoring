using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Core.Helpers;

public static class RuleValueHelper
{
    public static int? Compare(object? value1, object? value2)
    {
        if (value1 == null || value2 == null)
        {
            return null;
        }

        if (TryGetNumber(value1, out var number1) && TryGetNumber(value2, out var number2))
        {
            return number1.CompareTo(number2);
        }

        if (TryGetTimestamp(value1, out var timestamp1) && TryGetTimestamp(value2, out var timestamp2))
        {
            return timestamp1.CompareTo(timestamp2);
        }

        if (value1 is string string1 && value2 is string string2)
        {
            return string.Compare(string1, string2, StringComparison.Ordinal);
        }

        if (value1 is bool bool1 && value2 is bool bool2)
        {
            return bool1.CompareTo(bool2);
        }

        return null;
    }

    public static bool AreEqual(object? value1, object? value2)
    {
        if (value1 == null || value2 == null)
        {
            return value1 == null && value2 == null;
        }

        if (TryGetNumber(value1, out var number1) && TryGetNumber(value2, out var number2))
        {
            return number1 == number2;
        }

        if (TryGetTimestamp(value1, out var timestamp1) && TryGetTimestamp(value2, out var timestamp2))
        {
            return timestamp1 == timestamp2;
        }

        if (value1 is string string1 && value2 is string string2)
        {
            return string.Equals(string1, string2, StringComparison.Ordinal);
        }

        if (value1 is bool bool1 && value2 is bool bool2)
        {
            return bool1 == bool2;
        }

        return false;
    }

    public static bool TryGetNumber(object? value, out decimal number)
    {
        switch (value)
        {
            case decimal decimalValue:
                number = decimalValue;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case int intValue:
                number = intValue;
                return true;
            case short shortValue:
                number = shortValue;
                return true;
            case byte byteValue:
                number = byteValue;
                return true;
            case float floatValue:
                return TryGetNumber((double)floatValue, out number);
            case double doubleValue when !double.IsNaN(doubleValue) &&
                                         doubleValue >= (double)decimal.MinValue &&
                                         doubleValue <= (double)decimal.MaxValue:
                number = (decimal)doubleValue;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    // A timestamp without an offset is read as UTC, not server local time.
    public static bool TryGetDateTime(object? value, out DateTimeOffset dateTimeOffset)
    {
        dateTimeOffset = default;

        return value is string text &&
               DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTimeOffset);
    }

    private static bool TryGetTimestamp(object? value, out DateTimeOffset timestamp)
    {
        timestamp = default;

        return value is string text && StartsWithIsoDate(text) && TryGetDateTime(text, out timestamp);
    }

    private static bool StartsWithIsoDate(string text)
    {
        return text.Length >= 10 &&
               text[4] == '-' &&
               text[7] == '-' &&
               text[..4].All(char.IsAsciiDigit) &&
               text[5..7].All(char.IsAsciiDigit) &&
               text[8..10].All(char.IsAsciiDigit);
    }

    public static object? Extract(object? value)
    {
        return value switch
        {
            JsonValue jsonValue => jsonValue.GetValueKind() switch
            {
                JsonValueKind.String => jsonValue.GetValue<string>(),
                JsonValueKind.Number => ExtractJsonNumber(jsonValue),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => ExtractJsonArray(jsonValue),
                _ => null
            },
            JsonElement jsonElement => jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue) ? longValue : jsonElement.TryGetDouble(out var doubleValue) ? doubleValue : null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(x => Extract(x)).ToList(),
                _ => null
            },
            JsonArray jsonArray => jsonArray.Select(x => Extract(x)).ToList(),
            _ => value
        };
    }

    private static object ExtractJsonNumber(JsonValue jsonValue)
    {
        if (jsonValue.TryGetValue<long>(out var longValue))
        {
            return longValue;
        }

        if (jsonValue.TryGetValue<double>(out var doubleValue))
        {
            return doubleValue;
        }

        return jsonValue.GetValue<object>();
    }

    private static List<object?> ExtractJsonArray(JsonValue jsonValue)
    {
        if (jsonValue.GetValueKind() == JsonValueKind.Array)
        {
            return jsonValue.AsArray().Select(Extract).ToList();
        }

        throw new ArgumentException("Value is not an array.");
    }
}
