using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Validators.RuleSets;

public class RecoverRuleSetRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly RecoverRuleSetRequestValidator _sut;

    public RecoverRuleSetRequestValidatorTests()
    {
        _mockRuleSetService = new Mock<IRuleSetService>();
        _sut = new RecoverRuleSetRequestValidator(_mockRuleSetService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverRuleSetRequestDto.CreateValid(Fixture) with
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
    public async Task ValidateAsync_RuleSetIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverRuleSetRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_RuleSetIdExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeRecoverRuleSetRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RuleSetId);
    }
}