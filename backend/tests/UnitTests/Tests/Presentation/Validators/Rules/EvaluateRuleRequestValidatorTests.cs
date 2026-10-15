using System.Net;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.Models.RuleSets;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class EvaluateRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly EvaluateRuleRequestValidator _sut;

    public EvaluateRuleRequestValidatorTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockRuleSetService = new Mock<IRuleSetService>();
        _sut = new EvaluateRuleRequestValidator(_mockRuleService.Object, _mockRuleSetService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleRequestDto.CreateValid(Fixture) with
        {
            RuleId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Rule not found");
    }

    [Fact]
    public async Task ValidateAsync_RuleIsNotEnabled_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRule.CreateValid(Fixture) with { IsEnabled = new IsEnabled(false) });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorMessage($"{nameof(Rule)} is not enabled");
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIsNotEnabled_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRule.CreateValid(Fixture) with { IsEnabled = new IsEnabled(true) });

        _mockRuleSetService
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleSet.CreateValid(Fixture) with { IsEnabled = new IsEnabled(false) });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorMessage($"{nameof(RuleSet)} is not enabled");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRule.CreateValid(Fixture) with { IsEnabled = new IsEnabled(true) });

        _mockRuleSetService
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleSet.CreateValid(Fixture) with { IsEnabled = new IsEnabled(true) });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
