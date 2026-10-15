using System.Net;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.Models.RuleSets;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class CreateRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly CreateRuleRequestValidator _sut;

    public CreateRuleRequestValidatorTests()
    {
        _mockRuleSetService = new Mock<IRuleSetService>();
        _mockRuleService = new Mock<IRuleService>();
        _sut = new CreateRuleRequestValidator(_mockRuleSetService.Object, _mockRuleService.Object);
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                FriendlyName = string.Empty
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.FriendlyName)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                FriendlyName = new string('A', 51)
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.FriendlyName)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleBodyIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                RuleBody = null!
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.RuleBody)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture) with
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
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_RuleSetExceedsMaxRules_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(CreateRuleRequestValidator.MaxRulesInRuleSet));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleSetId)
            .WithErrorMessage($"{nameof(RuleSet)} count cannot be greater than {CreateRuleRequestValidator.MaxRulesInRuleSet}");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateRuleRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(0));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}