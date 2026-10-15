using Core.Extensions;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Extensions;

public class ObjectExtensionsTests : BaseTestClass
{
    [Fact]
    public void Serialize_GivenObject_ReturnsSerializedObject()
    {
        // Arrange
        var data = new { foo = "bar" };

        // Act
        var serializedData = data.Serialize();

        // Assert
        serializedData.Should().Be("{\"foo\":\"bar\"}");
    }
}