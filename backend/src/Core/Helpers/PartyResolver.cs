using System.Globalization;
using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.Parties;
using Core.Models.PartyMappings;
using Core.ValueObjects;

namespace Core.Helpers;

public static class PartyResolver
{
    public static List<ResolvedParty> Resolve(JsonObject payload, List<PartyRoleMapping> mappings)
    {
        var resolved = new List<ResolvedParty>();
        var seen = new HashSet<(PartyRole Role, string Identifier)>();

        foreach (var mapping in mappings)
        {
            foreach (var root in Roots(payload, mapping.Path))
            {
                if (!TryReadIdentifier(root, mapping.IdentifierPath, out var identifier))
                {
                    continue;
                }

                if (!seen.Add((mapping.Role, identifier)))
                {
                    continue;
                }

                resolved.Add(new ResolvedParty
                {
                    Role = mapping.Role,
                    Identifier = new PartyIdentifier(identifier),
                    DisplayName = ReadText(root, mapping.DisplayNamePath),
                    Attributes = ReadAttributes(root, mapping.AttributePaths)
                });
            }
        }

        return resolved;
    }

    private static List<PartyAttributeValue> ReadAttributes(JsonObject root, List<PartyAttributeMapping>? attributePaths)
    {
        var attributes = new List<PartyAttributeValue>();

        foreach (var attributePath in attributePaths ?? [])
        {
            if (ReadText(root, attributePath.Path) is { } value)
            {
                attributes.Add(new PartyAttributeValue { Name = attributePath.Name, Value = value });
            }
        }

        return attributes;
    }

    private static List<JsonObject> Roots(JsonObject payload, PartyPath? path)
    {
        if (!path.HasValue)
        {
            return [payload];
        }

        return PayloadPath.ResolveAll(payload, path.Value)
            .OfType<JsonObject>()
            .ToList();
    }

    private static bool TryReadIdentifier(JsonObject root, PartyPath identifierPath, out string identifier)
    {
        identifier = string.Empty;

        if (!TryReadText(root, identifierPath, out var value) || value.Length > PartyIdentifier.MaxLength)
        {
            return false;
        }

        identifier = value;

        return true;
    }

    private static PartyDisplayName? ReadText(JsonObject root, PartyPath? path)
    {
        if (!path.HasValue || !TryReadText(root, path.Value, out var value))
        {
            return null;
        }

        return new PartyDisplayName(value.Length > PartyDisplayName.MaxLength
            ? value[..PartyDisplayName.MaxLength]
            : value);
    }

    private static bool TryReadText(JsonObject root, PartyPath path, out string text)
    {
        text = string.Empty;

        var value = RuleValueHelper.Extract(PayloadPath.ResolveSingle(root, path));

        switch (value)
        {
            case string stringValue when !string.IsNullOrWhiteSpace(stringValue):
                text = stringValue.Trim();
                return true;
            case long or double or decimal:
                text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return text.Length > 0;
            default:
                return false;
        }
    }
}
