using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Validators.RuleSets;

public class EvaluateRuleSetRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly EvaluateRuleSetRequestValidator _sut;

    public EvaluateRuleSetRequestValidatorTests()
    {
        _mockRuleSetService = new Mock<IRuleSetService>();
        _sut = new EvaluateRuleSetRequestValidator(_mockRuleSetService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleSetRequestDto.CreateValid(Fixture) with
        {
            RuleSetId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleSetId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleSetDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleSetRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleSetId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Rule Set not found");
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIsDisabled_FailsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleSetRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleSetService
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleSet.CreateValid(Fixture) with { IsEnabled = new IsEnabled(false) });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleSetId)
            .WithErrorMessage("RuleSet is not enabled");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeEvaluateRuleSetRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleSetService
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleSet.CreateValid(Fixture) with { IsEnabled = new IsEnabled(true) });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RuleSetId);
    }
}