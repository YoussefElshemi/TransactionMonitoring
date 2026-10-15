using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.OutboxMessages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Core.Services;

public class WebhookConfigurationServiceTests : BaseTestClass
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly WebhookConfigurationService _sut;
    private readonly Mock<IWebhookConfigurationRepository> _webhookConfigurationRepository = new();
    private readonly Mock<IWebhookService> _webhookService = new();

    public WebhookConfigurationServiceTests()
    {
        _sut = new WebhookConfigurationService(
            _webhookConfigurationRepository.Object,
            _webhookService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task TestAsync_GivenWebhookConfigurationId_SendTestWebhook()
    {
        // Arrange
        var webhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture);

        _webhookConfigurationRepository
            .Setup(x => x.GetAsync(It.IsAny<WebhookConfigurationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookConfiguration);

        _webhookService
            .Setup(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationDeliveryResult
            {
                Result = new Result(true),
                StatusCode = new StatusCode(200),
                ResponseBody = null,
                Latency = new LatencyMilliseconds(9)
            });

        // Act
        var result = await _sut.TestAsync(webhookConfiguration.WebhookConfigurationId, CancellationToken.None);

        // Assert
        result.Should().Be(new Result(true));

        _webhookConfigurationRepository.Verify(x => x.GetAsync(It.IsAny<WebhookConfigurationId>(), It.IsAny<CancellationToken>()), Times.Once);
        _webhookService.Verify(x => x.SendAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}