using System.Text.Json.Nodes;
using Core.Enums;
using Core.Extensions;
using Core.Models.AuditLogs;
using FluentAssertions;
using Infrastructure.Entities;
using Infrastructure.Mappers;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class AuditedChangeMapperTests
{
    [Fact]
    public void Apply_PlainProperty_LeavesFieldAndValuesUnchanged()
    {
        // Arrange
        var change = new AuditLogChange
        {
            Field = "FriendlyName",
            OldValue = JsonValue.Create("Old"),
            NewValue = JsonValue.Create("New")
        };
        AuditedProperty[] properties = ["FriendlyName"];

        // Act
        var result = AuditedChangeMapper.Apply([change], properties);

        // Assert
        result.Should().ContainSingle();
        result[0].Field.Should().Be("FriendlyName");
        result[0].OldValue!.GetValue<string>().Should().Be("Old");
        result[0].NewValue!.GetValue<string>().Should().Be("New");
    }

    [Fact]
    public void Apply_MappedProperty_ProjectsValuesAndRelabelsField()
    {
        // Arrange
        var change = new AuditLogChange
        {
            Field = "StatusId",
            OldValue = JsonValue.Create(1),
            NewValue = JsonValue.Create(2)
        };
        AuditedProperty[] properties =
        [
            AuditedProperty.Mapped<int, string>("StatusId", id => ((AuditOperation)id).ToString(), "Status")
        ];

        // Act
        var result = AuditedChangeMapper.Apply([change], properties);

        // Assert
        result[0].Field.Should().Be("Status");
        result[0].OldValue!.GetValue<string>().Should().Be("Created");
        result[0].NewValue!.GetValue<string>().Should().Be("Updated");
    }

    [Fact]
    public void Apply_MappedProperty_Base64DecodesValues()
    {
        // Arrange
        var change = new AuditLogChange
        {
            Field = "RuleBody",
            OldValue = null,
            NewValue = JsonValue.Create("SGVsbG8=")
        };
        AuditedProperty[] properties =
        [
            AuditedProperty.Mapped<string, string>("RuleBody", body => body.FromBase64String())
        ];

        // Act
        var result = AuditedChangeMapper.Apply([change], properties);

        // Assert
        result[0].Field.Should().Be("RuleBody");
        result[0].OldValue.Should().BeNull();
        result[0].NewValue!.GetValue<string>().Should().Be("Hello");
    }
}
