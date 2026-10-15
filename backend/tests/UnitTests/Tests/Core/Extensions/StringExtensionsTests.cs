using Core.Extensions;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Extensions;

public class StringExtensionsTests : BaseTestClass
{
    [Fact]
    public void ToBase64String_GivenString_ReturnsBase64String()
    {
        // Arrange
        const string value = "test";

        // Act
        var response = value.ToBase64String();

        // Assert
        response.Should().Be("dGVzdA==");
    }

    [Fact]
    public void FromBase64String_GivenBase64String_String()
    {
        // Arrange
        const string value = "dGVzdA==";

        // Act
        var response = value.FromBase64String();

        // Assert
        response.Should().Be("test");
    }

    [Fact]
    public void TryDeserialize_GivenDeserializableString_ReturnsObject()
    {
        // Arrange
        const string value = "{\"foo\": \"bar\"}";

        // Act
        var response = value.TryDeserialize<Dictionary<string, string>>();

        // Assert
        response.Keys.Should().Contain("foo");
        response.Values.Should().Contain("bar");
    }

    [Fact]
    public void TryDeserialize_GivenNonDeserializableString_DoesThrow()
    {
        // Arrange
        const string value = "test";

        // Act && Assert
        Assert.Throws<InvalidOperationException>(() => value.TryDeserialize<Dictionary<string, string>>());
    }
}