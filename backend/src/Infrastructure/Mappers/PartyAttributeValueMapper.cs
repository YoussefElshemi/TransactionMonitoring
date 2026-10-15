using Core.Extensions;
using Core.Models.Parties;
using Core.ValueObjects;

namespace Infrastructure.Mappers;

public static class PartyAttributeValueMapper
{
    public static List<PartyAttributeValue> AttributesToModel(this string attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes))
        {
            return [];
        }

        return attributes.FromBase64String().TryDeserialize<List<PartyAttributeValueModel>>().ConvertAll(x => new PartyAttributeValue
        {
            Name = new PartyAttributeName(x.Name ?? string.Empty),
            Value = new PartyDisplayName(x.Value ?? string.Empty)
        });
    }

    public static string ToBase64String(this List<PartyAttributeValue> attributes)
    {
        return attributes
            .ConvertAll(x => new PartyAttributeValueModel { Name = x.Name, Value = x.Value })
            .Serialize()
            .ToBase64String();
    }
}
