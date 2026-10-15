using Core.Interfaces.Services;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleEvaluations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleEvaluations;

namespace UnitTests.Tests.Presentation.Validators.RuleEvaluations;

public class ListRuleEvaluationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleEvaluationService> _mockRuleEvaluationService;
    private readonly ListRuleEvaluationRequestValidator _sut;

    public ListRuleEvaluationRequestValidatorTests()
    {
        _mockRuleEvaluationService = new Mock<IRuleEvaluationService>();
        _sut = new ListRuleEvaluationRequestValidator(_mockRuleEvaluationService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListRuleEvaluationRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleEvaluationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_InvalidPagedRequest_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleEvaluationRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleEvaluationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPage_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleEvaluationRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 5,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleEvaluationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}
