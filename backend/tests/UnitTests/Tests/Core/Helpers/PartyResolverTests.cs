using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PartyResolverTests : BaseTestClass
{
    [Fact]
    public void Resolve_GivenAFlatPayloadAndNoPath_ReadsFromTheRoot()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "senderAccountId": "ACC-1", "senderName": "Ada" }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Remitter, null, "senderAccountId", "senderName") };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().ContainSingle();
        resolved[0].Role.Should().Be(PartyRole.Remitter);
        ((string)resolved[0].Identifier).Should().Be("ACC-1");
        resolved[0].DisplayName!.Value.ToString().Should().Be("Ada");
    }

    [Fact]
    public void Resolve_GivenANestedPath_ReadsRelativeToThePartyObject()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "sender": { "accountId": "ACC-1", "name": "Ada" } }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Remitter, "sender", "accountId", "name") };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().ContainSingle();
        ((string)resolved[0].Identifier).Should().Be("ACC-1");
        resolved[0].DisplayName!.Value.ToString().Should().Be("Ada");
    }

    [Fact]
    public void Resolve_GivenAWildcardPath_PairsEachIdentifierWithItsOwnName()
    {
        // Arrange
        var payload = JsonNode.Parse(
            """{ "parties": [ { "iban": "IBAN-1" }, { "iban": "IBAN-2", "name": "Grace" } ] }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Beneficiary, "parties[*]", "iban", "name") };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().HaveCount(2);
        resolved[0].DisplayName.Should().BeNull();
        resolved[1].DisplayName!.Value.ToString().Should().Be("Grace");
    }

    [Fact]
    public void Resolve_GivenAPathThatFindsNothing_RecordsNoParty()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "amount": 100 }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Remitter, "sender", "accountId", null) };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_GivenOneIdentifierUnderTwoRoles_ResolvesItTwice()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "sender": { "id": "ACC-1" }, "beneficiary": { "id": "ACC-1" } }""")!.AsObject();
        var mappings = new List<PartyRoleMapping>
        {
            Mapping(PartyRole.Remitter, "sender", "id", null),
            Mapping(PartyRole.Beneficiary, "beneficiary", "id", null)
        };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().HaveCount(2);
        resolved.Select(x => x.Role).Should().Equal(PartyRole.Remitter, PartyRole.Beneficiary);
        resolved.Select(x => (string)x.Identifier).Should().AllBe("ACC-1");
    }

    [Fact]
    public void Resolve_GivenTheSameIdentifierTwiceUnderOneRole_ResolvesItOnce()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "parties": [ { "iban": "IBAN-1" }, { "iban": "IBAN-1" } ] }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Beneficiary, "parties[*]", "iban", null) };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().ContainSingle();
    }

    [Fact]
    public void Resolve_GivenANumericIdentifier_ReadsItAsText()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "sender": { "accountId": 4915 } }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Remitter, "sender", "accountId", null) };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        ((string)resolved.Single().Identifier).Should().Be("4915");
    }

    [Theory]
    [InlineData("""{ "sender": { "accountId": true } }""")]
    [InlineData("""{ "sender": { "accountId": null } }""")]
    [InlineData("""{ "sender": { "accountId": "   " } }""")]
    [InlineData("""{ "sender": { "accountId": { "nested": "ACC-1" } } }""")]
    public void Resolve_GivenAValueThatCannotIdentifyAnybody_RecordsNoParty(string json)
    {
        // Arrange
        var payload = JsonNode.Parse(json)!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Remitter, "sender", "accountId", null) };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_GivenAnIdentifierLongerThanTheIndexTakes_RecordsNoParty()
    {
        // Arrange
        var identifier = new string('x', PartyIdentifier.MaxLength + 1);
        var payload = JsonNode.Parse($$"""{ "sender": { "accountId": "{{identifier}}" } }""")!.AsObject();
        var mappings = new List<PartyRoleMapping> { Mapping(PartyRole.Remitter, "sender", "accountId", null) };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_GivenNoMappings_ResolvesNobody()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "sender": { "accountId": "ACC-1" } }""")!.AsObject();

        // Act
        var resolved = PartyResolver.Resolve(payload, []);

        // Assert
        resolved.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_GivenAttributePaths_CarriesThemOntoTheParty()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "sender": { "accountId": "ACC-1", "country": "GB", "tier": 3 } }""")!.AsObject();

        var mappings = new List<PartyRoleMapping>
        {
            Mapping(PartyRole.Remitter, "sender", "accountId", null) with
            {
                AttributePaths =
                [
                    new PartyAttributeMapping { Name = new PartyAttributeName("country"), Path = new PartyPath("country") },
                    new PartyAttributeMapping { Name = new PartyAttributeName("tier"), Path = new PartyPath("tier") }
                ]
            }
        };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Single().Attributes.Select(x => ((string)x.Name, (string)x.Value))
            .Should().BeEquivalentTo([("country", "GB"), ("tier", "3")]);
    }

    [Fact]
    public void Resolve_GivenAnAttributeThePayloadOmits_LeavesItOut()
    {
        // Arrange
        var payload = JsonNode.Parse("""{ "sender": { "accountId": "ACC-1" } }""")!.AsObject();

        var mappings = new List<PartyRoleMapping>
        {
            Mapping(PartyRole.Remitter, "sender", "accountId", null) with
            {
                AttributePaths = [new PartyAttributeMapping { Name = new PartyAttributeName("country"), Path = new PartyPath("country") }]
            }
        };

        // Act
        var resolved = PartyResolver.Resolve(payload, mappings);

        // Assert
        resolved.Single().Attributes.Should().BeEmpty();
    }

    private static PartyRoleMapping Mapping(PartyRole role, string? path, string identifierPath, string? displayNamePath)
    {
        return new PartyRoleMapping
        {
            Role = role,
            Path = path is null ? null : new PartyPath(path),
            IdentifierPath = new PartyPath(identifierPath),
            DisplayNamePath = displayNamePath is null ? null : new PartyPath(displayNamePath)
        };
    }
}
