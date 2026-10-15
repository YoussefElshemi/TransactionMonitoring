using System.Text.Json.Nodes;

namespace Core.Helpers;

public static class PayloadMasker
{
    public const string Redacted = "••••";

    public static JsonObject Mask(JsonObject payload)
    {
        var clone = payload.DeepClone().AsObject();

        Descend(clone);

        return clone;
    }

    private static void Descend(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    var replacement = Replacement(property.Value);

                    if (replacement is null)
                    {
                        Descend(property.Value);

                        continue;
                    }

                    jsonObject[property.Key] = replacement;
                }

                return;

            case JsonArray jsonArray:
                for (var index = 0; index < jsonArray.Count; index++)
                {
                    var replacement = Replacement(jsonArray[index]);

                    if (replacement is null)
                    {
                        Descend(jsonArray[index]);

                        continue;
                    }

                    jsonArray[index] = replacement;
                }

                return;
        }
    }

    private static JsonNode? Replacement(JsonNode? node)
    {
        if (node is not JsonValue jsonValue)
        {
            return null;
        }

        if (jsonValue.TryGetValue<string>(out var value) && CardTokeniser.IsToken(value))
        {
            var lastFour = CardTokeniser.LastFourOf(value);

            return JsonValue.Create(lastFour is null ? Redacted : Redacted + " " + lastFour);
        }

        return JsonValue.Create(Redacted);
    }
}
