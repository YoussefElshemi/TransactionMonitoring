using Core.Interfaces.Services;
using Core.Models.Alerts;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using Presentation.Factories;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Presentation.Factories;

public class AlertResponseFactoryTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<IRuleService> _mockRuleService = new();
    private readonly Mock<ITransactionService> _mockTransactionService = new();
    private readonly AlertResponseFactory _sut;

    public AlertResponseFactoryTests()
    {
        var relatedRecordLoader = new RelatedRecordLoader(
            _mockOrganizationService.Object,
            new Mock<IApplicationUserService>().Object,
            _mockRuleService.Object,
            new Mock<IRuleSetService>().Object,
            _mockTransactionService.Object,
            new Mock<IRuleVersionService>().Object,
            new Mock<IRuleDeploymentService>().Object,
            new Mock<IApiClientService>().Object,
            new Mock<ISuspiciousActivityReportService>().Object,
            MockTimeProvider);

        _sut = new AlertResponseFactory(relatedRecordLoader);
    }

    [Fact]
    public async Task CreateAsync_NoAlerts_LoadsNothing()
    {
        // Act
        var resources = await _sut.CreateAsync([], Context(FakeOrganizationId.CreateValid(Fixture)), CancellationToken.None);

        // Assert
        resources.Should().BeEmpty();

        _mockOrganizationService.Verify(
            x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<OrganizationId>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRuleService.Verify(
            x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleId>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockTransactionService.Verify(
            x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<TransactionId>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EnrichesTheAlertWithItsRuleAndTransaction()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var alert = FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId };

        SetUpRelatedRecords(alert, organizationId);

        // Act
        var resource = await _sut.CreateAsync(alert, Context(organizationId), CancellationToken.None);

        // Assert
        resource.Alert.AlertId.Should().Be(alert.AlertId);
        resource.Organization.Should().NotBeNull();
        resource.Rule.Should().NotBeNull();
        resource.Transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ManyAlerts_LoadsEachRelatedRecordOnce()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var ruleId = FakeRuleId.CreateValid(Fixture);
        var transactionId = FakeTransactionId.CreateValid(Fixture);

        var alerts = Enumerable.Range(0, 5).Select(_ => FakeAlert.CreateValid(Fixture) with
        {
            OrganizationId = organizationId,
            RuleId = ruleId,
            TransactionId = transactionId
        }).ToList();

        SetUpRelatedRecords(alerts[0], organizationId);

        // Act
        var resources = await _sut.CreateAsync(alerts, Context(organizationId), CancellationToken.None);

        // Assert
        resources.Should().HaveCount(5);

        _mockOrganizationService.Verify(
            x => x.ListByIdsAsync(It.Is<IReadOnlyCollection<OrganizationId>>(y => y.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleService.Verify(
            x => x.ListByIdsAsync(It.Is<IReadOnlyCollection<RuleId>>(y => y.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionService.Verify(
            x => x.ListByIdsAsync(It.Is<IReadOnlyCollection<TransactionId>>(y => y.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RelatedRecordsOwnedByAnotherOrganization_AreNotDisclosed()
    {
        // Arrange
        var owningOrganizationId = FakeOrganizationId.CreateValid(Fixture);
        var callerOrganizationId = FakeOrganizationId.CreateValid(Fixture);
        var alert = FakeAlert.CreateValid(Fixture) with { OrganizationId = owningOrganizationId };

        SetUpRelatedRecords(alert, owningOrganizationId);

        // Act
        var resource = await _sut.CreateAsync(alert, Context(callerOrganizationId), CancellationToken.None);

        // Assert
        resource.Alert.AlertId.Should().Be(alert.AlertId);
        resource.Organization.Should().BeNull();
        resource.Rule.Should().BeNull();
        resource.Transaction.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AdminCaller_SeesRecordsFromAnyOrganization()
    {
        // Arrange
        var owningOrganizationId = FakeOrganizationId.CreateValid(Fixture);
        var alert = FakeAlert.CreateValid(Fixture) with { OrganizationId = owningOrganizationId };

        SetUpRelatedRecords(alert, owningOrganizationId);

        var context = new EnrichmentContext
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            IsAdmin = true
        };

        // Act
        var resource = await _sut.CreateAsync(alert, context, CancellationToken.None);

        // Assert
        resource.Organization.Should().NotBeNull();
        resource.Rule.Should().NotBeNull();
        resource.Transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_RelatedRecordsMissing_StillReturnsTheAlert()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var alert = FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId };

        _mockOrganizationService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<OrganizationId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mockRuleService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mockTransactionService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<TransactionId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var resource = await _sut.CreateAsync(alert, Context(organizationId), CancellationToken.None);

        // Assert
        resource.Alert.AlertId.Should().Be(alert.AlertId);
        resource.Organization.Should().BeNull();
        resource.Rule.Should().BeNull();
        resource.Transaction.Should().BeNull();
    }

    private void SetUpRelatedRecords(Alert alert, OrganizationId owningOrganizationId)
    {
        var organization = FakeOrganization.CreateValid(Fixture) with
        {
            OrganizationId = owningOrganizationId
        };

        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleId = alert.RuleId,
            OrganizationId = owningOrganizationId
        };

        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            TransactionId = alert.TransactionId,
            OrganizationId = owningOrganizationId
        };

        _mockOrganizationService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<OrganizationId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([organization]);

        _mockRuleService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);

        _mockTransactionService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<TransactionId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([transaction]);
    }

    private static EnrichmentContext Context(OrganizationId organizationId)
    {
        return new EnrichmentContext
        {
            OrganizationId = organizationId,
            IsAdmin = false
        };
    }
}
