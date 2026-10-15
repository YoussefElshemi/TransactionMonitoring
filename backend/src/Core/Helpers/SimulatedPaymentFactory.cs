using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.PartyMappings;
using Core.Models.Simulations;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Helpers;

public static class SimulatedPaymentFactory
{
    private const int PartyPoolSize = 12;

    private static readonly string[] Currencies = ["GBP", "EUR", "USD"];
    private static readonly string[] Countries = ["GB", "IE", "DE", "FR", "US", "NL"];
    private static readonly string[] Methods = ["card", "bank_transfer", "wallet"];

    private static readonly string[] GivenNames =
        ["Alex", "Sam", "Jordan", "Riley", "Casey", "Morgan", "Avery", "Quinn", "Rowan", "Skyler", "Emerson", "Harper"];

    private static readonly string[] FamilyNames =
        ["Okafor", "Nakamura", "Silva", "Novak", "Haddad", "Lindqvist", "Moreau", "Kowalski", "Rossi", "Ahmed", "Dubois", "Bergman"];

    public static List<SimulatedPayment> Create(SimulationPlan simulationPlan)
    {
        var random = new Random(simulationPlan.Seed);
        var parties = CreateParties(random);
        var payments = new List<SimulatedPayment>(simulationPlan.PaymentCount);

        for (var index = 0; index < simulationPlan.PaymentCount; index++)
        {
            var scenario = ScenarioFor(simulationPlan.Scenario, index, simulationPlan.PaymentCount, random);
            var shape = ShapeFor(scenario, index, parties, random, simulationPlan.GeneratedAt);

            var payload = simulationPlan.TransactionSchema is { } transactionSchema
                ? BuildToSchema(transactionSchema.Fields, shape, random)
                : BuildDefault(shape);

            ApplyPartyMapping(payload, simulationPlan.PartyMapping, shape);

            payments.Add(new SimulatedPayment
            {
                Payload = payload,
                ExternalReference = new ExternalReference($"sim_{simulationPlan.SimulationId}_{index:D6}")
            });
        }

        return payments;
    }

    private static SimulationScenario ScenarioFor(SimulationScenario scenario, int index, int total, Random random)
    {
        if (scenario != SimulationScenario.Mixed)
        {
            return scenario;
        }

        var structuringWindow = Math.Max(3, total / 6);
        var velocityWindow = Math.Max(3, total / 8);

        if (index < structuringWindow)
        {
            return SimulationScenario.Structuring;
        }

        if (index < structuringWindow + velocityWindow)
        {
            return SimulationScenario.VelocitySpike;
        }

        return random.Next(10) == 0 ? SimulationScenario.HighValue : SimulationScenario.Baseline;
    }

    private static PaymentShape ShapeFor(
        SimulationScenario scenario,
        int index,
        List<SimulatedParty> parties,
        Random random,
        EvaluatedAt generatedAt)
    {
        var occurredAt = ((DateTimeOffset)generatedAt).ToString("O");

        return scenario switch
        {
            SimulationScenario.Structuring => new PaymentShape
            {
                Remitter = parties[0],
                Beneficiary = parties[1 + index % (parties.Count - 1)],
                Amount = 9000m + random.Next(0, 900),
                Currency = Currencies[0],
                Method = Methods[1],
                OccurredAt = occurredAt
            },

            SimulationScenario.VelocitySpike => new PaymentShape
            {
                Remitter = parties[1],
                Beneficiary = parties[2 + index % (parties.Count - 2)],
                Amount = 40m + random.Next(0, 260),
                Currency = Currencies[random.Next(Currencies.Length)],
                Method = Methods[0],
                OccurredAt = occurredAt
            },

            SimulationScenario.HighValue => new PaymentShape
            {
                Remitter = parties[random.Next(parties.Count)],
                Beneficiary = parties[random.Next(parties.Count)],
                Amount = 75_000m + random.Next(0, 425_000),
                Currency = Currencies[random.Next(Currencies.Length)],
                Method = Methods[1],
                OccurredAt = occurredAt
            },

            _ => new PaymentShape
            {
                Remitter = parties[random.Next(parties.Count)],
                Beneficiary = parties[random.Next(parties.Count)],
                Amount = Math.Round(5m + (decimal)(random.NextDouble() * 1_200), 2),
                Currency = Currencies[random.Next(Currencies.Length)],
                Method = Methods[random.Next(Methods.Length)],
                OccurredAt = occurredAt
            }
        };
    }

    private static JsonObject BuildDefault(PaymentShape shape)
    {
        return new JsonObject
        {
            ["amount"] = JsonValue.Create(shape.Amount),
            ["currency"] = JsonValue.Create(shape.Currency),
            ["method"] = JsonValue.Create(shape.Method),
            ["remitter"] = new JsonObject
            {
                ["accountId"] = JsonValue.Create(shape.Remitter.Identifier),
                ["name"] = JsonValue.Create(shape.Remitter.DisplayName),
                ["country"] = JsonValue.Create(shape.Remitter.Country)
            },
            ["beneficiary"] = new JsonObject
            {
                ["accountId"] = JsonValue.Create(shape.Beneficiary.Identifier),
                ["name"] = JsonValue.Create(shape.Beneficiary.DisplayName),
                ["country"] = JsonValue.Create(shape.Beneficiary.Country)
            }
        };
    }

    private static JsonObject BuildToSchema(List<SchemaField> fields, PaymentShape shape, Random random)
    {
        var payload = new JsonObject();

        foreach (var field in fields)
        {
            payload[field.Name] = ValueFor(field, shape, random);
        }

        return payload;
    }

    private static JsonNode ValueFor(SchemaField field, PaymentShape shape, Random random)
    {
        if (field.AllowedValues is { Count: > 0 } allowedValues)
        {
            return JsonValue.Create(allowedValues[random.Next(allowedValues.Count)]);
        }

        return field.Type switch
        {
            SchemaFieldType.Number => JsonValue.Create(NumberFor(field.Name, shape, random)),
            SchemaFieldType.Boolean => JsonValue.Create(random.Next(2) == 0),
            SchemaFieldType.DateTime => JsonValue.Create(shape.OccurredAt),
            SchemaFieldType.Object => BuildToSchema(field.Fields ?? [], shape, random),
            SchemaFieldType.Array => ArrayFor(field, shape, random),
            _ => JsonValue.Create(StringFor(field.Name, shape, random))
        };
    }

    private static JsonArray ArrayFor(SchemaField field, PaymentShape shape, Random random)
    {
        var elementType = field.ElementType ?? SchemaFieldType.String;

        var element = elementType == SchemaFieldType.Object
            ? BuildToSchema(field.Fields ?? [], shape, random)
            : ValueFor(field with { Type = elementType, Fields = null, ElementType = null }, shape, random);

        return element is null ? [] : [element];
    }

    private static decimal NumberFor(string name, PaymentShape shape, Random random)
    {
        return Mentions(name, "amount", "value", "total", "sum") ? shape.Amount : random.Next(1, 100);
    }

    private static string StringFor(string name, PaymentShape shape, Random random)
    {
        if (Mentions(name, "currency", "ccy"))
        {
            return shape.Currency;
        }

        if (Mentions(name, "country", "iso"))
        {
            return random.Next(2) == 0 ? shape.Remitter.Country : shape.Beneficiary.Country;
        }

        if (Mentions(name, "method", "channel", "type"))
        {
            return shape.Method;
        }

        if (Mentions(name, "beneficiary", "payee", "creditor"))
        {
            return Mentions(name, "name") ? shape.Beneficiary.DisplayName : shape.Beneficiary.Identifier;
        }

        if (Mentions(name, "remitter", "payer", "debtor", "sender", "account"))
        {
            return Mentions(name, "name") ? shape.Remitter.DisplayName : shape.Remitter.Identifier;
        }

        if (Mentions(name, "name"))
        {
            return shape.Remitter.DisplayName;
        }

        if (Mentions(name, "reference", "description", "narrative", "note"))
        {
            return $"Simulated {shape.Method} payment";
        }

        return $"sim-{random.Next(100_000, 999_999)}";
    }

    private static bool Mentions(string name, params string[] words)
    {
        return words.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyPartyMapping(JsonObject payload, PartyMapping? partyMapping, PaymentShape shape)
    {
        if (partyMapping is null)
        {
            return;
        }

        foreach (var mapping in partyMapping.Mappings)
        {
            var party = mapping.Role == PartyRole.Beneficiary ? shape.Beneficiary : shape.Remitter;
            var root = mapping.Path?.ToString();

            Write(payload, Join(root, mapping.IdentifierPath), party.Identifier);

            if (mapping.DisplayNamePath is { } displayNamePath)
            {
                Write(payload, Join(root, displayNamePath), party.DisplayName);
            }

            foreach (var attribute in mapping.AttributePaths ?? [])
            {
                Write(payload, Join(root, attribute.Path), party.Country);
            }
        }
    }

    private static string Join(string? root, PartyPath path)
    {
        return string.IsNullOrWhiteSpace(root) ? path.ToString() : root + "." + path;
    }

    private static void Write(JsonObject payload, string path, string value)
    {
        if (path.Contains('[') || path.Contains('*'))
        {
            return;
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return;
        }

        var current = payload;

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (current[segments[index]] is not JsonObject child)
            {
                child = new JsonObject();
                current[segments[index]] = child;
            }

            current = child;
        }

        current[segments[^1]] = JsonValue.Create(value);
    }

    private static List<SimulatedParty> CreateParties(Random random)
    {
        var parties = new List<SimulatedParty>(PartyPoolSize);

        for (var index = 0; index < PartyPoolSize; index++)
        {
            parties.Add(new SimulatedParty
            {
                Identifier = $"SIM{random.Next(10_000_000, 99_999_999)}",
                DisplayName = $"{GivenNames[index % GivenNames.Length]} {FamilyNames[(index * 7) % FamilyNames.Length]}",
                Country = Countries[index % Countries.Length]
            });
        }

        return parties;
    }

    private sealed record SimulatedParty
    {
        public required string Identifier { get; init; }
        public required string DisplayName { get; init; }
        public required string Country { get; init; }
    }

    private sealed record PaymentShape
    {
        public required SimulatedParty Remitter { get; init; }
        public required SimulatedParty Beneficiary { get; init; }
        public required decimal Amount { get; init; }
        public required string Currency { get; init; }
        public required string Method { get; init; }
        public required string OccurredAt { get; init; }
    }
}
