using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Validators.RuleSets;

public class UpdateRuleSetRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly UpdateRuleSetRequestValidator _sut;

    public UpdateRuleSetRequestValidatorTests()
    {
        _mockRuleSetService = new Mock<IRuleSetService>();
        _sut = new UpdateRuleSetRequestValidator(_mockRuleSetService.Object, new Mock<ISubOrganizationService>().Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleSetRequestDto.CreateValid(Fixture) with
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
        var request = FakeUpdateRuleSetRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_RuleSetExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleSetRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RuleSetId);
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleSetRequestDto.CreateValid(Fixture);
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
        var request = FakeUpdateRuleSetRequestDto.CreateValid(Fixture);
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
    public async Task ValidateAsync_FriendlyNameIsValid_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleSetRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.FriendlyName);
    }
}