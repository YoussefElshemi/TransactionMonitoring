using Core.Configs;
using Core.Constants;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Core.Services;

public class NotificationServiceTests : BaseTestClass
{
    private readonly Mock<ICaseRepository> _mockCaseRepository = new();
    private readonly Mock<ISuspiciousActivityReportRepository> _mockSuspiciousActivityReportRepository = new();
    private readonly Mock<INotificationSubscriptionRepository> _mockNotificationSubscriptionRepository = new();
    private readonly Mock<IWebhookConfigurationRepository> _mockWebhookConfigurationRepository = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _mockNotificationSubscriptionRepository
            .Setup(x => x.ListEnabledAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockDisplayPreferenceService
            .Setup(x => x.ForOrganizationAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDisplayPreferences.CreateValid());

        _sut = new NotificationService(
            _mockCaseRepository.Object,
            _mockSuspiciousActivityReportRepository.Object,
            _mockNotificationSubscriptionRepository.Object,
            _mockWebhookConfigurationRepository.Object,
            _mockOutboxMessageService.Object,
            _mockDisplayPreferenceService.Object,
            _mockUnitOfWork.Object,
            Options.Create(new AppConfig()),
            MockTimeProvider);
    }

    [Fact]
    public async Task NotifyBreachedAsync_GivenASubscribedEndpoint_RaisesTheBreachWebhook()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);
        var webhookConfiguration = SubscribedTo(@case.OrganizationId, WebhookEvents.CaseBreached);

        _mockCaseRepository
            .Setup(x => x.ListBreachNotificationCandidatesAsync(It.IsAny<Count>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([@case]);

        _mockWebhookConfigurationRepository
            .Setup(x => x.GetAllAsync(@case.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([webhookConfiguration]);

        var staged = CaptureStaged();

        // Act
        await _sut.NotifyBreachedAsync(CancellationToken.None);

        // Assert
        staged.Should().ContainSingle();
        staged[0].Event!.Value.ToString().Should().Be(WebhookEvents.CaseBreached);
        staged[0].WebhookConfigurationId.Should().Be(webhookConfiguration.WebhookConfigurationId);
    }

    [Fact]
    public async Task NotifyOverdueFilingsAsync_GivenASubscribedEndpoint_RaisesTheOverdueWebhook()
    {
        // Arrange
        var suspiciousActivityReport = FakeSuspiciousActivityReport.CreateValid(Fixture);
        var webhookConfiguration = SubscribedTo(
            suspiciousActivityReport.OrganizationId, WebhookEvents.SuspiciousActivityReportOverdue);

        _mockSuspiciousActivityReportRepository
            .Setup(x => x.ListOverdueNotificationCandidatesAsync(It.IsAny<Count>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([suspiciousActivityReport]);

        _mockWebhookConfigurationRepository
            .Setup(x => x.GetAllAsync(suspiciousActivityReport.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([webhookConfiguration]);

        var staged = CaptureStaged();

        // Act
        await _sut.NotifyOverdueFilingsAsync(CancellationToken.None);

        // Assert
        staged.Should().ContainSingle();
        staged[0].Event!.Value.ToString().Should().Be(WebhookEvents.SuspiciousActivityReportOverdue);
        staged[0].WebhookConfigurationId.Should().Be(webhookConfiguration.WebhookConfigurationId);
    }

    [Fact]
    public async Task NotifyOverdueFilingsAsync_GivenAnEndpointSubscribedToSomethingElse_RaisesNothing()
    {
        // Arrange
        var suspiciousActivityReport = FakeSuspiciousActivityReport.CreateValid(Fixture);
        var webhookConfiguration = SubscribedTo(suspiciousActivityReport.OrganizationId, WebhookEvents.CaseCreated);

        _mockSuspiciousActivityReportRepository
            .Setup(x => x.ListOverdueNotificationCandidatesAsync(It.IsAny<Count>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([suspiciousActivityReport]);

        _mockWebhookConfigurationRepository
            .Setup(x => x.GetAllAsync(suspiciousActivityReport.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([webhookConfiguration]);

        var staged = CaptureStaged();

        // Act
        await _sut.NotifyOverdueFilingsAsync(CancellationToken.None);

        // Assert
        staged.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyOverdueFilingsAsync_GivenManyReportsForOneOrganisation_ReadsTheEndpointsOnce()
    {
        // Arrange
        var first = FakeSuspiciousActivityReport.CreateValid(Fixture);
        var second = FakeSuspiciousActivityReport.CreateValid(Fixture) with { OrganizationId = first.OrganizationId };

        _mockSuspiciousActivityReportRepository
            .Setup(x => x.ListOverdueNotificationCandidatesAsync(It.IsAny<Count>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        _mockWebhookConfigurationRepository
            .Setup(x => x.GetAllAsync(first.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([SubscribedTo(first.OrganizationId, WebhookEvents.SuspiciousActivityReportOverdue)]);

        var staged = CaptureStaged();

        // Act
        await _sut.NotifyOverdueFilingsAsync(CancellationToken.None);

        // Assert
        staged.Should().HaveCount(2);
        _mockWebhookConfigurationRepository.Verify(
            x => x.GetAllAsync(first.OrganizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private WebhookConfiguration SubscribedTo(OrganizationId organizationId, string @event)
    {
        return FakeWebhookConfiguration.CreateValid(Fixture) with
        {
            OrganizationId = organizationId,
            SubscribedEvents = [new WebhookEvent(@event)]
        };
    }

    private List<CreateOutboxMessageRequest> CaptureStaged()
    {
        var staged = new List<CreateOutboxMessageRequest>();

        _mockOutboxMessageService
            .Setup(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<List<CreateOutboxMessageRequest>, CancellationToken>((x, _) => staged.AddRange(x));

        return staged;
    }
}
