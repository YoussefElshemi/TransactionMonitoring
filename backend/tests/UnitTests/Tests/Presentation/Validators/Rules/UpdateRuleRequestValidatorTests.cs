using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Models.Rules;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class UpdateRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly UpdateRuleRequestValidator _sut;

    public UpdateRuleRequestValidatorTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _sut = new UpdateRuleRequestValidator(_mockRuleService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_FriendlyNameTooLong_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleRequestDto.CreateInvalid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.FriendlyName)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_NoFieldsProvided_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleRequestDto.CreateValid(Fixture) with
        {
            Body = new UpdateRuleRequestBodyDto
            {
                FriendlyName = null,
                IsEnabled = null
            }
        };

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body)
            .WithErrorMessage("At least one field must be provided");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
