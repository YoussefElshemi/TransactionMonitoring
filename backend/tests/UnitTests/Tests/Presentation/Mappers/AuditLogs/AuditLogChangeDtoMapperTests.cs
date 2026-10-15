using System.Text.Json.Nodes;
using Core.Models.AuditLogs;
using FluentAssertions;
using Presentation.Mappers.AuditLogs;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Mappers.AuditLogs;

public class AuditLogChangeDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_AuditLogChange()
    {
        // Arrange
        var change = new AuditLogChange
        {
            Field = "FriendlyName",
            OldValue = JsonValue.Create("Old name"),
            NewValue = JsonValue.Create("New name")
        };

        // Act
        var dto = change.ToDto();

        // Assert
        dto.Field.Should().Be(change.Field);
        dto.OldValue.Should().BeSameAs(change.OldValue);
        dto.NewValue.Should().BeSameAs(change.NewValue);
    }

    [Fact]
    public void ToDto_AuditLogChange_WithNullOldValue()
    {
        // Arrange
        var change = new AuditLogChange
        {
            Field = "IsDeleted",
            OldValue = null,
            NewValue = JsonValue.Create(true)
        };

        // Act
        var dto = change.ToDto();

        // Assert
        dto.Field.Should().Be(change.Field);
        dto.OldValue.Should().BeNull();
        dto.NewValue!.GetValue<bool>().Should().BeTrue();
    }
}
