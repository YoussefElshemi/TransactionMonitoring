using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;

namespace UnitTests.Tests.Core.Mappers;

public class CreateOutboxMessageRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateOutboxMessageRequest()
    {
        // Arrange
        var createOutboxMessageRequest = FakeCreateOutboxMessageRequest.CreateValid(Fixture);

        // Act
        var outboxMessage = createOutboxMessageRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        outboxMessage.OutboxMessageId.Should().NotBeNull();
        outboxMessage.Payload.Should().Be(new Payload(createOutboxMessageRequest.Payload));
        outboxMessage.SecretKey.Should().Be(createOutboxMessageRequest.SecretKey);
        outboxMessage.Channel.Should().Be(createOutboxMessageRequest.Channel);
        outboxMessage.Destination.Should().Be(createOutboxMessageRequest.Destination);
        outboxMessage.Attempts.Should().Be(new Attempts(0));
        outboxMessage.IsProcessed.Should().Be(new IsProcessed(false));
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.IsDeleted.Should().Be(new IsDeleted(false));
        outboxMessage.DeletedAt.Should().BeNull();
    }
}