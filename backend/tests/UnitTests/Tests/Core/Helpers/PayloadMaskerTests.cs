using System.Text.Json.Nodes;
using Core.Extensions;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PayloadMaskerTests : BaseTestClass
{
    [Fact]
    public void Mask_WithholdsEveryScalarWhereverItSits()
    {
        // Arrange
        var payload = """
            {
              "amount": 24500,
              "currency": "GBP",
              "sender": { "name": "Bob Smith", "account": "GB29NWBK60161331926819" },
              "beneficiaries": [ { "name": "Alice Jones" } ],
              "urgent": true
            }
            """.TryDeserialize<JsonObject>();

        // Act
        var masked = PayloadMasker.Mask(payload);

        // Assert
        var serialized = masked.Serialize();
        serialized.Should().NotContain("Bob Smith");
        serialized.Should().NotContain("Alice Jones");
        serialized.Should().NotContain("GB29NWBK60161331926819");
        serialized.Should().NotContain("24500");
        serialized.Should().NotContain("GBP");
    }

    [Fact]
    public void Mask_KeepsTheShapeOfThePayment()
    {
        // Arrange
        var payload = """
            {
              "amount": 24500,
              "sender": { "name": "Bob Smith" },
              "beneficiaries": [ { "name": "Alice Jones" }, { "name": "Carol Wu" } ]
            }
            """.TryDeserialize<JsonObject>();

        // Act
        var masked = PayloadMasker.Mask(payload);

        // Assert
        masked.Should().ContainKey("amount");
        masked["sender"]!.AsObject().Should().ContainKey("name");
        masked["beneficiaries"]!.AsArray().Should().HaveCount(2);
        masked["sender"]!["name"]!.GetValue<string>().Should().Be(PayloadMasker.Redacted);
    }

    [Fact]
    public void Mask_ShowsTheLastFourOfACardToken()
    {
        // Arrange
        var payload = $$"""{"card":"{{CardTokeniser.Tokenise(DataKeyWrapper.Create(), "4111111111111111")}}"}"""
            .TryDeserialize<JsonObject>();

        // Act
        var masked = PayloadMasker.Mask(payload);

        // Assert
        masked["card"]!.GetValue<string>().Should().Be(PayloadMasker.Redacted + " 1111");
    }

    [Fact]
    public void Mask_LeavesAnAbsentValueAbsent()
    {
        // Arrange
        var payload = """{"reviewedBy":null}""".TryDeserialize<JsonObject>();

        // Act
        var masked = PayloadMasker.Mask(payload);

        // Assert
        masked["reviewedBy"].Should().BeNull();
    }

    [Fact]
    public void Mask_DoesNotMutateTheCallersPayload()
    {
        // Arrange
        var payload = """{"sender":{"name":"Bob Smith"}}""".TryDeserialize<JsonObject>();

        // Act
        PayloadMasker.Mask(payload);

        // Assert
        payload["sender"]!["name"]!.GetValue<string>().Should().Be("Bob Smith");
    }
}
