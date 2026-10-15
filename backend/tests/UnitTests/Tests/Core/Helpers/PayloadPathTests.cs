using System.Text.Json.Nodes;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PayloadPathTests : BaseTestClass
{
    [Theory]
    [InlineData("amount")]
    [InlineData("card.bin")]
    [InlineData("parties[0].country")]
    [InlineData("parties[*].country")]
    [InlineData("matrix[0][1]")]
    public void TryParse_GivenAReadablePath_Succeeds(string path)
    {
        // Act
        var parsed = PayloadPath.TryParse(path, out _);

        // Assert
        parsed.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("parties[")]
    [InlineData("parties[]")]
    [InlineData("parties[-1]")]
    [InlineData("parties[a]")]
    [InlineData("parties[*]extra")]
    [InlineData("card..bin")]
    [InlineData("[0]")]
    public void TryParse_GivenAnUnreadablePath_Fails(string path)
    {
        // Act
        var parsed = PayloadPath.TryParse(path, out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void TryParse_GivenMoreSegmentsThanTheEngineWalks_Fails()
    {
        // Arrange
        var path = string.Join('.', Enumerable.Range(0, PayloadPath.MaxSegments + 1).Select(x => $"level{x}"));

        // Act
        var parsed = PayloadPath.TryParse(path, out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Theory]
    [InlineData("parties[*].country", true)]
    [InlineData("parties[0].country", false)]
    [InlineData("amount", false)]
    public void HasWildcard_ReportsWhetherThePathResolvesToMany(string path, bool expected)
    {
        // Act
        var actual = PayloadPath.HasWildcard(path);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ResolveSingle_GivenADottedPath_ReadsTheNestedValue()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["card"] = new JsonObject { ["bin"] = "453201" }
        };

        // Act
        var actual = PayloadPath.ResolveSingle(payload, "card.bin");

        // Assert
        actual!.GetValue<string>().Should().Be("453201");
    }

    [Fact]
    public void ResolveSingle_GivenAnIndexedPath_ReadsThatElement()
    {
        // Arrange
        var payload = Parties();

        // Act
        var actual = PayloadPath.ResolveSingle(payload, "parties[1].country");

        // Assert
        actual!.GetValue<string>().Should().Be("IR");
    }

    [Fact]
    public void ResolveSingle_GivenAnIndexPastTheEndOfTheArray_ReadsNothing()
    {
        // Arrange
        var payload = Parties();

        // Act
        var actual = PayloadPath.ResolveSingle(payload, "parties[9].country");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void ResolveAll_GivenAWildcard_ReadsEveryElement()
    {
        // Arrange
        var payload = Parties();

        // Act
        var actual = PayloadPath.ResolveAll(payload, "parties[*].country");

        // Assert
        actual.Should().HaveCount(2);
        actual.ConvertAll(x => x!.GetValue<string>()).Should().Equal("GB", "IR");
    }

    [Fact]
    public void ResolveAll_GivenAWildcardOverElementsThatDoNotAllCarryTheField_SkipsTheOnesThatDoNot()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["country"] = "GB" },
                new JsonObject { ["name"] = "Anonymous" })
        };

        // Act
        var actual = PayloadPath.ResolveAll(payload, "parties[*].country");

        // Assert
        actual.Should().ContainSingle();
    }

    [Fact]
    public void ResolveAll_GivenAPathIntoSomethingThatIsNotAnObject_ReadsNothing()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["card"] = "453201"
        };

        // Act
        var actual = PayloadPath.ResolveAll(payload, "card.bin");

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public void ResolveSingle_GivenAKeyThatItselfCarriesADot_PrefersTheLiteralKey()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["card.bin"] = "literal",
            ["card"] = new JsonObject { ["bin"] = "nested" }
        };

        // Act
        var actual = PayloadPath.ResolveSingle(payload, "card.bin");

        // Assert
        actual!.GetValue<string>().Should().Be("literal");
    }

    private static JsonObject Parties()
    {
        return new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["country"] = "GB" },
                new JsonObject { ["country"] = "IR" })
        };
    }
}
