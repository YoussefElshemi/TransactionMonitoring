using Core.Configs;
using Core.Interfaces.Services;
using Core.Models.OutboxMessages;
using Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;

namespace UnitTests.Tests.Infrastructure.BackgroundServices;

public class OutboxBackgroundServiceTests : BaseTestClass
{
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly OutboxBackgroundService _sut;

    public OutboxBackgroundServiceTests()
    {
        var scopeFactory = SetUpServiceScopeFactory(
            (typeof(IOutboxMessageService), _mockOutboxMessageService.Object)
        );

        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture));

        _sut = new OutboxBackgroundService(scopeFactory, _mockConfig.Object, MockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_OutboxDisabled_OutboxMessageServiceIsNotCalled()
    {
        // Arrange
        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture) with
            {
                BackgroundServiceConfig = FakeBackgroundServiceConfig.CreateValid(Fixture) with
                {
                    OutboxEnabled = false
                }
            });

        // Act
        await _sut.StartAsync(CancellationToken.None);

        // Assert
        _mockOutboxMessageService
            .Verify(x => x.ListPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_OutboxMessageServiceIsNotCalled()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // Act
        await _sut.StartAsync(source.Token);

        // Assert
        _mockOutboxMessageService
            .Verify(x => x.ListPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoPendingMessages_OutboxMessageNotProcessed()
    {
        // Arrange
        _mockOutboxMessageService
            .Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.StartAsync(CancellationToken.None);

        // Assert
        _mockOutboxMessageService
            .Verify(x => x.ListPendingAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageService
            .Verify(x => x.ProcessAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PendingMessages_OutboxMessageProcessed()
    {
        // Arrange
        _mockOutboxMessageService
            .Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeOutboxMessage.CreateValid(Fixture)]);

        // Act
        await _sut.StartAsync(CancellationToken.None);

        // Assert
        _mockOutboxMessageService
            .Verify(x => x.ListPendingAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageService
            .Verify(x => x.ProcessAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessingOutboxMessageThrowsException_ExceptionCaughtAndLogged()
    {
        // Arrange
        _mockOutboxMessageService
            .Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeOutboxMessage.CreateValid(Fixture)]);

        _mockOutboxMessageService
            .Setup(x => x.ProcessAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        // Act
        await _sut.StartAsync(CancellationToken.None);

        // Assert
        _mockOutboxMessageService
            .Verify(x => x.ListPendingAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageService
            .Verify(x => x.ProcessAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        MockLogger.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessingOutboxBatchThrowsException_ExceptionCaughtAndLogged()
    {
        // Arrange
        _mockOutboxMessageService
            .Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        // Act
        await _sut.StartAsync(CancellationToken.None);

        // Assert
        _mockOutboxMessageService
            .Verify(x => x.ListPendingAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageService
            .Verify(x => x.ProcessAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);

        MockLogger.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}