using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class AuditLogMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_AuditLog()
    {
        // Arrange
        var auditLogEntity = FakeAuditLogEntity.CreateValid(Fixture);
        var changes = new List<AuditLogChange>
        {
            new()
            {
                Field = "FriendlyName",
                OldValue = JsonValue.Create("Old"),
                NewValue = JsonValue.Create("New")
            }
        };

        // Act
        var auditLog = auditLogEntity.ToModel(changes);

        // Assert
        auditLog.AuditLogId.Should().Be(new AuditLogId(auditLogEntity.AuditLogId));
        auditLog.EntityType.Should().Be((AuditableEntityType)auditLogEntity.EntityTypeId);
        auditLog.EntityId.Should().Be(auditLogEntity.EntityId);
        auditLog.OrganizationId.Should().Be(new OrganizationId(auditLogEntity.OrganizationId!.Value));
        auditLog.Operation.Should().Be((AuditOperation)auditLogEntity.OperationId);
        auditLog.Changes.Should().BeSameAs(changes);
        auditLog.ChangedByUserId.Should().Be(new ApplicationUserId(auditLogEntity.ChangedByUserId!.Value));
        auditLog.CreatedAt.Should().Be(new CreatedAt(auditLogEntity.CreatedAt));
    }
}
