using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class RecoverRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly RecoverRuleRequestValidator _sut;

    public RecoverRuleRequestValidatorTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _sut = new RecoverRuleRequestValidator(_mockRuleService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverRuleRequestDto.CreateValid(Fixture) with
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
        var request = FakeRecoverRuleRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeRecoverRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
