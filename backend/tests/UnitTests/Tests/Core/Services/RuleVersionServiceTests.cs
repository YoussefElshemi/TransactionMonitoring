using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class RuleVersionServiceTests : BaseTestClass
{
    private readonly Mock<IRuleRepository> _mockRuleRepository = new();
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository = new();
    private readonly Mock<ITransactionSchemaService> _mockTransactionSchemaService = new();
    private readonly Mock<IPartyMappingService> _mockPartyMappingService = new();
    private readonly Mock<IVelocityProfileService> _mockVelocityProfileService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly RuleVersionService _sut;

    public RuleVersionServiceTests()
    {
        _sut = new RuleVersionService(
            _mockRuleRepository.Object,
            _mockRuleVersionRepository.Object,
            _mockTransactionSchemaService.Object,
            _mockPartyMappingService.Object,
            _mockVelocityProfileService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task CreateVersionAsync_CreatesNextVersionWithoutDeploying()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with { CurrentVersionNumber = new VersionNumber(3) };
        var createRuleVersionRequest = new CreateRuleVersionRequest
        {
            RuleId = rule.RuleId,
            RuleBody = FakeRuleBody.CreateValid(Fixture),
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskWeight = new RiskWeight(0),
            CreatedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        RuleVersion? addedVersion = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleVersionRepository
            .Setup(x => x.GetMaxVersionNumberAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VersionNumber(3));

        _mockRuleVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<RuleVersion>(), It.IsAny<CancellationToken>()))
            .Callback<RuleVersion, CancellationToken>((v, _) => addedVersion = v);

        // Act
        await _sut.CreateVersionAsync(createRuleVersionRequest, CancellationToken.None);

        // Assert
        addedVersion.Should().NotBeNull();
        addedVersion!.VersionNumber.Should().Be(new VersionNumber(4));
        addedVersion.RuleId.Should().Be(rule.RuleId);
        addedVersion.CreatedByUserId.Should().Be(createRuleVersionRequest.CreatedByUserId);
        _mockRuleRepository.Verify(x => x.DeployVersionAsync(It.IsAny<RuleId>(), It.IsAny<RuleVersion>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
