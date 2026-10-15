using System.Text.Json.Nodes;
using Core.Services;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Services;

public class AuditLogChangeCalculatorTests : BaseTestClass
{
    [Fact]
    public void Compute_WithNoPreviousSnapshot_EmitsEveryFieldWithNullOldValue()
    {
        // Arrange
        var current = new JsonObject
        {
            ["FriendlyName"] = "Name",
            ["IsEnabled"] = true,
            ["CurrentVersionNumber"] = 1
        };

        // Act
        var changes = AuditLogChangeCalculator.Compute(null, current);

        // Assert
        changes.Should().HaveCount(3);
        changes.Should().OnlyContain(x => x.OldValue == null);
        changes.Should().Contain(x => x.Field == "FriendlyName" && x.NewValue!.GetValue<string>() == "Name");
        changes.Should().Contain(x => x.Field == "IsEnabled" && x.NewValue!.GetValue<bool>());
        changes.Should().Contain(x => x.Field == "CurrentVersionNumber" && x.NewValue!.GetValue<int>() == 1);
    }

    [Fact]
    public void Compute_WithUpdatedSnapshot_EmitsOnlyChangedFields()
    {
        // Arrange
        var previous = new JsonObject
        {
            ["FriendlyName"] = "Old name",
            ["IsEnabled"] = true,
            ["CurrentVersionNumber"] = 2
        };

        var current = new JsonObject
        {
            ["FriendlyName"] = "New name",
            ["IsEnabled"] = true,
            ["CurrentVersionNumber"] = 3
        };

        // Act
        var changes = AuditLogChangeCalculator.Compute(previous, current);

        // Assert
        changes.Should().HaveCount(2);
        changes.Should().Contain(x =>
            x.Field == "FriendlyName" &&
            x.OldValue!.GetValue<string>() == "Old name" &&
            x.NewValue!.GetValue<string>() == "New name");
        changes.Should().Contain(x =>
            x.Field == "CurrentVersionNumber" &&
            x.OldValue!.GetValue<int>() == 2 &&
            x.NewValue!.GetValue<int>() == 3);
        changes.Should().NotContain(x => x.Field == "IsEnabled");
    }

    [Fact]
    public void Compute_WithDeletion_EmitsIsDeletedTransition()
    {
        // Arrange
        var previous = new JsonObject
        {
            ["FriendlyName"] = "Name",
            ["IsDeleted"] = false
        };

        var current = new JsonObject
        {
            ["FriendlyName"] = "Name",
            ["IsDeleted"] = true
        };

        // Act
        var changes = AuditLogChangeCalculator.Compute(previous, current);

        // Assert
        changes.Should().ContainSingle();
        changes.Should().Contain(x =>
            x.Field == "IsDeleted" &&
            !x.OldValue!.GetValue<bool>() &&
            x.NewValue!.GetValue<bool>());
    }

    [Fact]
    public void Compute_WithNoFieldLevelChanges_ReturnsEmpty()
    {
        // Arrange
        var previous = new JsonObject
        {
            ["FriendlyName"] = "Name",
            ["IsEnabled"] = true
        };

        var current = new JsonObject
        {
            ["FriendlyName"] = "Name",
            ["IsEnabled"] = true
        };

        // Act
        var changes = AuditLogChangeCalculator.Compute(previous, current);

        // Assert
        changes.Should().BeEmpty();
    }
}
