using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Helpers;

public static class PayloadTokeniser
{
    public static JsonObject Tokenise(DataKey dataKey, JsonObject payload)
    {
        var clone = payload.DeepClone().AsObject();

        Descend(dataKey, clone);

        return clone;
    }

    // System.Text.Json refuses to reassign a parented node, so containers are edited in place.
    private static void Descend(DataKey dataKey, JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    var replacement = Replacement(dataKey, property.Value);

                    if (replacement is null)
                    {
                        Descend(dataKey, property.Value);

                        continue;
                    }

                    jsonObject[property.Key] = replacement;
                }

                return;

            case JsonArray jsonArray:
                for (var index = 0; index < jsonArray.Count; index++)
                {
                    var replacement = Replacement(dataKey, jsonArray[index]);

                    if (replacement is null)
                    {
                        Descend(dataKey, jsonArray[index]);

                        continue;
                    }

                    jsonArray[index] = replacement;
                }

                return;
        }
    }

    private static JsonNode? Replacement(DataKey dataKey, JsonNode? node)
    {
        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var value) ||
            !CardNumberDetector.IsCardNumber(value) ||
            !CardNumberDetector.TryNormalise(value, out var digits))
        {
            return null;
        }

        return JsonValue.Create(CardTokeniser.Tokenise(dataKey, digits));
    }
}
