using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleEvaluations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleEvaluations;

namespace UnitTests.Tests.Presentation.Validators.RuleEvaluations;

public class RuleEvaluationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleEvaluationService> _mockRuleEvaluationService;
    private readonly RuleEvaluationRequestValidator _sut;

    public RuleEvaluationRequestValidatorTests()
    {
        _mockRuleEvaluationService = new Mock<IRuleEvaluationService>();
        _sut = new RuleEvaluationRequestValidator(_mockRuleEvaluationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleEvaluationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRuleEvaluationRequestDto.CreateValid(Fixture) with
        {
            RuleEvaluationId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleEvaluationId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleEvaluationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeRuleEvaluationRequestDto.CreateValid(Fixture);

        _mockRuleEvaluationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleEvaluationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleEvaluationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Rule Evaluation not found");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeRuleEvaluationRequestDto.CreateValid(Fixture);

        _mockRuleEvaluationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleEvaluationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
