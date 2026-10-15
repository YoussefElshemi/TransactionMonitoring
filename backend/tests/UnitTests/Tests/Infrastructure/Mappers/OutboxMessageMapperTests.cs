using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class OutboxMessageMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_OutboxMessage()
    {
        // Arrange
        var outboxMessageEntity = FakeOutboxMessageEntity.CreateValid(Fixture);

        // Act
        var outboxMessage = outboxMessageEntity.ToModel();

        // Assert
        outboxMessage.OutboxMessageId.Should().Be(new OutboxMessageId(outboxMessageEntity.OutboxMessageId));
        outboxMessage.Payload.Should().Be(new Payload(outboxMessageEntity.Payload));
        outboxMessage.CreatedAt.Should().Be(new CreatedAt(outboxMessageEntity.CreatedAt));
        outboxMessage.UpdatedAt.Should().Be(new UpdatedAt(outboxMessageEntity.UpdatedAt));
        outboxMessage.IsDeleted.Should().Be(new IsDeleted(outboxMessageEntity.IsDeleted));
        outboxMessage.DeletedAt.Should().Be(new DeletedAt(outboxMessageEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_OutboxMessage()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        // Act
        var outboxMessageEntity = outboxMessage.ToEntity();

        // Assert
        outboxMessageEntity.OutboxMessageId.Should().Be(outboxMessage.OutboxMessageId);
        outboxMessageEntity.Payload.Should().Be(outboxMessage.Payload);
        outboxMessageEntity.CreatedAt.Should().Be(outboxMessage.CreatedAt);
        outboxMessageEntity.UpdatedAt.Should().Be(outboxMessage.UpdatedAt);
        outboxMessageEntity.IsDeleted.Should().Be(outboxMessage.IsDeleted);
        outboxMessageEntity.DeletedAt.Should().Be(outboxMessage.DeletedAt!.Value);
    }
}