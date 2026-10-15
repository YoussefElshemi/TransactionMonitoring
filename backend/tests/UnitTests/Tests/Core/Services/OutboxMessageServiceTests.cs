using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.OutboxMessages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;

namespace UnitTests.Tests.Core.Services;

public class OutboxMessageServiceTests : BaseTestClass
{
    private readonly Mock<IOutboxMessageRepository> _mockOutboxMessageRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<INotificationSender> _mockNotificationSender = new();
    private readonly Mock<IWebhookHealthService> _mockWebhookHealthService = new();
    private readonly OutboxMessageService _sut;

    public OutboxMessageServiceTests()
    {
        _mockNotificationSender.SetupGet(x => x.Channel).Returns(NotificationChannel.Webhook);

        _sut = new OutboxMessageService(
            [_mockNotificationSender.Object],
            _mockOutboxMessageRepository.Object,
            _mockWebhookHealthService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task ProcessAsync_GivenOutboxMessage_SuccessfullySendsWebhook()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        _mockNotificationSender
            .Setup(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Delivered(new StatusCode(200)));

        // Act
        await _sut.ProcessAsync(outboxMessage, CancellationToken.None);

        // Assert
        _mockNotificationSender.Verify(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockOutboxMessageRepository.Verify(x => x.ProcessAsync(
            It.IsAny<OutboxMessageId>(), It.IsAny<UpdateOutboxMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockOutboxMessageRepository.Verify(x => x.UpdateAsync(It.IsAny<UpdateOutboxMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_GivenOutboxMessage_FailsToSendWebhook()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);
        _mockNotificationSender
            .Setup(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Failed(new LatencyMilliseconds(12), "connection refused"));

        // Act
        await _sut.ProcessAsync(outboxMessage, CancellationToken.None);

        // Assert
        _mockNotificationSender.Verify(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockOutboxMessageRepository.Verify(x => x.ProcessAsync(
            It.IsAny<OutboxMessageId>(), It.IsAny<UpdateOutboxMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOutboxMessageRepository.Verify(x => x.UpdateAsync(It.IsAny<UpdateOutboxMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_GivenAResponse_RecordsWhatTheEndpointAnswered()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture) with { Attempts = new Attempts(2) };
        _mockNotificationSender
            .Setup(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationDeliveryResult
            {
                Result = new Result(false),
                StatusCode = new StatusCode(503),
                ResponseBody = new ResponseBody("upstream unavailable"),
                Latency = new LatencyMilliseconds(841)
            });

        UpdateOutboxMessageRequest? captured = null;
        _mockOutboxMessageRepository
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateOutboxMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateOutboxMessageRequest, CancellationToken>((x, _) => captured = x);

        // Act
        await _sut.ProcessAsync(outboxMessage, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        ((int)captured!.Attempts).Should().Be(3);
        ((int)captured.ResponseStatusCode!.Value).Should().Be(503);
        captured.ResponseBody!.Value.ToString().Should().Be("upstream unavailable");
        ((int)captured.Latency!.Value).Should().Be(841);
        captured.LastAttemptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_GivenAWebhookRow_ReportsTheOutcomeWithTheAttemptItProduced()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture) with { Attempts = new Attempts(4) };
        _mockNotificationSender
            .Setup(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Failed(new LatencyMilliseconds(3)));

        // Act
        await _sut.ProcessAsync(outboxMessage, CancellationToken.None);

        // Assert
        _mockWebhookHealthService.Verify(x => x.RecordOutcomeAsync(
            outboxMessage,
            new Result(false),
            It.Is<Attempts>(a => a == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_GivenASuppressedSandboxRow_NeitherSendsNorReportsHealth()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture) with
        {
            Mode = Mode.Test,
            Channel = NotificationChannel.Webhook
        };

        _mockNotificationSender.SetupGet(x => x.Channel).Returns(NotificationChannel.Email);

        var sut = new OutboxMessageService(
            [_mockNotificationSender.Object],
            _mockOutboxMessageRepository.Object,
            _mockWebhookHealthService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);

        // Act
        await sut.ProcessAsync(outboxMessage with { Channel = NotificationChannel.Email }, CancellationToken.None);

        // Assert
        _mockOutboxMessageRepository.Verify(x => x.SuppressAsync(It.IsAny<OutboxMessageId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockNotificationSender.Verify(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockWebhookHealthService.Verify(x => x.RecordOutcomeAsync(
            It.IsAny<OutboxMessage>(), It.IsAny<Result>(), It.IsAny<Attempts>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static NotificationDeliveryResult Delivered(StatusCode statusCode)
    {
        return new NotificationDeliveryResult
        {
            Result = new Result(true),
            StatusCode = statusCode,
            ResponseBody = null,
            Latency = new LatencyMilliseconds(5)
        };
    }
}
