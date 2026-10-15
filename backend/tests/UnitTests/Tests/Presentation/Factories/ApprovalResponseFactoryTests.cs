using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Approvals;
using Core.Models.RuleDeployments;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using Presentation.Factories;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Presentation.Factories;

public class ApprovalResponseFactoryTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<IApplicationUserService> _mockApplicationUserService = new();
    private readonly Mock<IRuleDeploymentService> _mockRuleDeploymentService = new();
    private readonly Mock<IRuleService> _mockRuleService = new();
    private readonly Mock<ISuspiciousActivityReportService> _mockSuspiciousActivityReportService = new();
    private readonly ApprovalResponseFactory _sut;

    public ApprovalResponseFactoryTests()
    {
        _mockOrganizationService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<OrganizationId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mockApplicationUserService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<ApplicationUserId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var relatedRecordLoader = new RelatedRecordLoader(
            _mockOrganizationService.Object,
            _mockApplicationUserService.Object,
            _mockRuleService.Object,
            new Mock<IRuleSetService>().Object,
            new Mock<ITransactionService>().Object,
            new Mock<IRuleVersionService>().Object,
            _mockRuleDeploymentService.Object,
            new Mock<IApiClientService>().Object,
            _mockSuspiciousActivityReportService.Object,
            MockTimeProvider);

        _sut = new ApprovalResponseFactory(relatedRecordLoader);
    }

    [Fact]
    public async Task CreateAsync_DeploymentApproval_EnrichesTheDeploymentAndItsRule()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var deployment = Deployment(organizationId);
        var approval = DeploymentApproval(organizationId, deployment);

        SetUpRelatedRecords(deployment, organizationId);

        // Act
        var resource = await _sut.CreateAsync(approval, Context(organizationId), CancellationToken.None);

        // Assert
        resource.RuleDeployment.Should().NotBeNull();
        resource.RuleDeployment!.RuleDeploymentId.Should().Be(deployment.RuleDeploymentId);
        resource.RuleDeployment.Strategy.Should().Be(deployment.Strategy.ToString());
        resource.RuleDeployment.StepPercentage.Should().Be(deployment.StepPercentage);
        resource.RuleDeployment.StepIntervalSeconds.Should().Be(deployment.StepIntervalSeconds);
        resource.Rule.Should().NotBeNull();
        resource.Rule!.RuleId.Should().Be(deployment.RuleId);
    }

    [Fact]
    public async Task CreateAsync_CaseResolutionApproval_CarriesNeitherKey()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var approval = FakeApproval.CreateValid(Fixture) with { OrganizationId = organizationId };

        // Act
        var resource = await _sut.CreateAsync(approval, Context(organizationId), CancellationToken.None);

        // Assert
        resource.RuleDeployment.Should().BeNull();
        resource.Rule.Should().BeNull();

        _mockRuleDeploymentService.Verify(
            x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleDeploymentId>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRuleService.Verify(
            x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleId>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ManyDeploymentApprovals_LoadsEachRelatedRecordOnce()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var deployment = Deployment(organizationId);

        var approvals = Enumerable.Range(0, 20)
            .Select(_ => DeploymentApproval(organizationId, deployment))
            .ToList();

        SetUpRelatedRecords(deployment, organizationId);

        // Act
        var resources = await _sut.CreateAsync(approvals, Context(organizationId), CancellationToken.None);

        // Assert
        resources.Should().HaveCount(20);
        resources.Should().OnlyContain(x => x.RuleDeployment != null && x.Rule != null);

        _mockRuleDeploymentService.Verify(
            x => x.ListByIdsAsync(It.Is<IReadOnlyCollection<RuleDeploymentId>>(y => y.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleService.Verify(
            x => x.ListByIdsAsync(It.Is<IReadOnlyCollection<RuleId>>(y => y.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DeploymentOwnedByAnotherOrganization_DisclosesNeitherKey()
    {
        // Arrange
        var owningOrganizationId = FakeOrganizationId.CreateValid(Fixture);
        var callerOrganizationId = FakeOrganizationId.CreateValid(Fixture);
        var deployment = Deployment(owningOrganizationId);
        var approval = DeploymentApproval(owningOrganizationId, deployment);

        SetUpRelatedRecords(deployment, owningOrganizationId);

        // Act
        var resource = await _sut.CreateAsync(approval, Context(callerOrganizationId), CancellationToken.None);

        // Assert
        resource.Approval.ApprovalId.Should().Be(approval.ApprovalId);
        resource.RuleDeployment.Should().BeNull();
        resource.Rule.Should().BeNull();
    }

    private RuleDeployment Deployment(OrganizationId organizationId)
    {
        return FakeRuleDeployment.CreateValid(Fixture) with { OrganizationId = organizationId };
    }

    private Approval DeploymentApproval(OrganizationId organizationId, RuleDeployment deployment)
    {
        return FakeApproval.CreateValid(Fixture) with
        {
            OrganizationId = organizationId,
            Subject = ApprovalSubject.RuleDeployment,
            RuleDeploymentId = deployment.RuleDeploymentId,
            CaseId = null,
            ProposedCaseResolution = null
        };
    }

    private void SetUpRelatedRecords(RuleDeployment deployment, OrganizationId owningOrganizationId)
    {
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleId = deployment.RuleId,
            OrganizationId = owningOrganizationId
        };

        _mockRuleDeploymentService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleDeploymentId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([deployment]);

        _mockRuleService
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<RuleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);
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
