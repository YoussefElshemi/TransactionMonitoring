using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.OutboxMessages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class WebhookDeliveryServiceTests : BaseTestClass
{
    private const int ReplayLimit = 2;

    private readonly Mock<IOutboxMessageRepository> _mockOutboxMessageRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly WebhookDeliveryService _sut;

    public WebhookDeliveryServiceTests()
    {
        _sut = new WebhookDeliveryService(
            _mockOutboxMessageRepository.Object,
            _mockUnitOfWork.Object,
            Options.Create(new AppConfig { WebhookConfig = new WebhookConfig { ReplayRangeLimit = ReplayLimit } }),
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task ReplayAsync_GivenADelivery_QueuesANewRowPointingAtTheOriginal()
    {
        // Arrange
        var original = FakeOutboxMessage.CreateValid(Fixture) with { ReplayOfOutboxMessageId = null };

        _mockOutboxMessageRepository
            .Setup(x => x.GetAsync(It.IsAny<OutboxMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        OutboxMessage? added = null;
        _mockOutboxMessageRepository
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((x, _) => added = x);

        // Act
        await _sut.ReplayAsync(original.OutboxMessageId, CancellationToken.None);

        // Assert
        added.Should().NotBeNull();
        added!.ReplayOfOutboxMessageId.Should().Be(original.OutboxMessageId);
        added.OutboxMessageId.Should().NotBe(original.OutboxMessageId);
        added.Payload.Should().Be(original.Payload);
        added.Event.Should().Be(original.Event);
        added.PayloadVersion.Should().Be(original.PayloadVersion);
        ((int)added.Attempts).Should().Be(0);
    }

    [Fact]
    public async Task ReplayAsync_GivenARowThatIsItselfAReplay_PointsAtTheOriginalRatherThanTheReplay()
    {
        // Arrange
        var root = FakeOutboxMessageId.CreateValid(Fixture);
        var original = FakeOutboxMessage.CreateValid(Fixture) with { ReplayOfOutboxMessageId = root };

        _mockOutboxMessageRepository
            .Setup(x => x.GetAsync(It.IsAny<OutboxMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        OutboxMessage? added = null;
        _mockOutboxMessageRepository
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((x, _) => added = x);

        // Act
        await _sut.ReplayAsync(original.OutboxMessageId, CancellationToken.None);

        // Assert
        added!.ReplayOfOutboxMessageId.Should().Be(root);
    }

    [Fact]
    public async Task ReplayAsync_GivenAnEmailRow_Refuses()
    {
        // Arrange
        var original = FakeOutboxMessage.CreateValid(Fixture) with { Channel = NotificationChannel.Email };

        _mockOutboxMessageRepository
            .Setup(x => x.GetAsync(It.IsAny<OutboxMessageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);

        // Act
        var act = async () => await _sut.ReplayAsync(original.OutboxMessageId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotReplayableException>();
    }

    [Fact]
    public async Task ReplayRangeAsync_GivenMoreMatchesThanTheCeiling_QueuesThePageAndSaysThereIsMore()
    {
        // Arrange
        var request = Request();

        _mockOutboxMessageRepository
            .Setup(x => x.CountForReplayAsync(It.IsAny<ReplayWebhookDeliveryRangeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Count(7));

        _mockOutboxMessageRepository
            .Setup(x => x.ListForReplayAsync(It.IsAny<ReplayWebhookDeliveryRangeRequest>(), It.IsAny<Count>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeOutboxMessage.CreateValid(Fixture), FakeOutboxMessage.CreateValid(Fixture)]);

        // Act
        var batch = await _sut.ReplayRangeAsync(request, CancellationToken.None);

        // Assert
        ((int)batch.Matched).Should().Be(7);
        ((int)batch.Queued).Should().Be(2);
        ((bool)batch.HasMore).Should().BeTrue();
    }

    [Fact]
    public async Task ReplayRangeAsync_GivenEverythingFits_SaysThereIsNoMore()
    {
        // Arrange
        var request = Request();

        _mockOutboxMessageRepository
            .Setup(x => x.CountForReplayAsync(It.IsAny<ReplayWebhookDeliveryRangeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Count(1));

        _mockOutboxMessageRepository
            .Setup(x => x.ListForReplayAsync(It.IsAny<ReplayWebhookDeliveryRangeRequest>(), It.IsAny<Count>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeOutboxMessage.CreateValid(Fixture)]);

        // Act
        var batch = await _sut.ReplayRangeAsync(request, CancellationToken.None);

        // Assert
        ((bool)batch.HasMore).Should().BeFalse();
    }

    [Fact]
    public async Task ReplayRangeAsync_GivenAWindowThatEndsBeforeItStarts_Refuses()
    {
        // Arrange
        var request = Request() with
        {
            From = new PeriodStart(MockTimeProvider.GetUtcNow()),
            To = new PeriodEnd(MockTimeProvider.GetUtcNow().AddDays(-1))
        };

        // Act
        var act = async () => await _sut.ReplayRangeAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidTimeRangeException>();
    }

    private ReplayWebhookDeliveryRangeRequest Request()
    {
        return new ReplayWebhookDeliveryRangeRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            From = new PeriodStart(MockTimeProvider.GetUtcNow().AddDays(-1)),
            To = new PeriodEnd(MockTimeProvider.GetUtcNow()),
            WebhookConfigurationId = null,
            Event = null,
            IncludeDelivered = new IncludeDelivered(false)
        };
    }
}
