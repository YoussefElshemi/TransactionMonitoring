using System.Text.Json.Nodes;
using Core.Extensions;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PayloadTokeniserTests : BaseTestClass
{
    private const string CardNumber = "4111111111111111";

    [Fact]
    public void Tokenise_ReplacesACardNumberWhereverItSits()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        var payload = $$"""
            {
              "amount": 24500,
              "instrument": { "number": "{{CardNumber}}" },
              "beneficiaries": [ { "card": "5555555555554444" } ]
            }
            """.TryDeserialize<JsonObject>();

        // Act
        var tokenised = PayloadTokeniser.Tokenise(dataKey, payload);

        // Assert
        var serialized = tokenised.Serialize();
        serialized.Should().NotContain(CardNumber);
        serialized.Should().NotContain("5555555555554444");
        CardTokeniser.IsToken(tokenised["instrument"]!["number"]!.GetValue<string>()).Should().BeTrue();
        CardTokeniser.IsToken(tokenised["beneficiaries"]![0]!["card"]!.GetValue<string>()).Should().BeTrue();
    }

    [Fact]
    public void Tokenise_LeavesEverythingThatIsNotACardNumberUntouched()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        var payload = """
            {
              "amount": 24500,
              "currency": "GBP",
              "reference": "INV-2026-0041",
              "account": "GB29NWBK60161331926819",
              "urgent": true,
              "reviewedBy": null
            }
            """.TryDeserialize<JsonObject>();

        // Act
        var tokenised = PayloadTokeniser.Tokenise(dataKey, payload);

        // Assert
        tokenised.Serialize().Should().Be(payload.Serialize());
    }

    [Fact]
    public void Tokenise_GivesTheSameCardTheSameTokenForOneOrganisation()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        var first = $$"""{"card":"{{CardNumber}}"}""".TryDeserialize<JsonObject>();
        var second = $$"""{"card":"{{CardNumber}}"}""".TryDeserialize<JsonObject>();

        // Act
        var tokenisedFirst = PayloadTokeniser.Tokenise(dataKey, first);
        var tokenisedSecond = PayloadTokeniser.Tokenise(dataKey, second);

        // Assert
        tokenisedFirst["card"]!.GetValue<string>().Should().Be(tokenisedSecond["card"]!.GetValue<string>());
    }

    [Fact]
    public void Tokenise_GivesTheSameCardDifferentTokensAcrossOrganisations()
    {
        // Arrange
        var payload = $$"""{"card":"{{CardNumber}}"}""".TryDeserialize<JsonObject>();

        // Act
        var first = PayloadTokeniser.Tokenise(DataKeyWrapper.Create(), payload);
        var second = PayloadTokeniser.Tokenise(DataKeyWrapper.Create(), payload);

        // Assert
        first["card"]!.GetValue<string>().Should().NotBe(second["card"]!.GetValue<string>());
    }

    [Fact]
    public void Tokenise_KeepsTheLastFourDigitsReadable()
    {
        // Arrange
        var payload = $$"""{"card":"{{CardNumber}}"}""".TryDeserialize<JsonObject>();

        // Act
        var tokenised = PayloadTokeniser.Tokenise(DataKeyWrapper.Create(), payload);

        // Assert
        var token = tokenised["card"]!.GetValue<string>();
        CardTokeniser.LastFourOf(token).Should().Be("1111");
    }

    [Fact]
    public void Tokenise_DoesNotMutateTheCallersPayload()
    {
        // Arrange
        var payload = $$"""{"card":"{{CardNumber}}"}""".TryDeserialize<JsonObject>();

        // Act
        PayloadTokeniser.Tokenise(DataKeyWrapper.Create(), payload);

        // Assert
        payload["card"]!.GetValue<string>().Should().Be(CardNumber);
    }

    [Fact]
    public void Tokenise_LeavesACardNumberSentAsANumberAlone()
    {
        // Arrange
        var payload = """{"card":4111111111111111}""".TryDeserialize<JsonObject>();

        // Act
        var tokenised = PayloadTokeniser.Tokenise(DataKeyWrapper.Create(), payload);

        // Assert
        tokenised.Serialize().Should().Be(payload.Serialize());
    }
}
