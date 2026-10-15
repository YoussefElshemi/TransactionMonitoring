using Core.Interfaces.Services;
using Core.Models.RuleSets;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Validators.RuleSets;

public class ListRuleSetRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly ListRuleSetRequestValidator _sut;

    public ListRuleSetRequestValidatorTests()
    {
        _mockRuleSetService = new Mock<IRuleSetService>();
        _sut = new ListRuleSetRequestValidator(_mockRuleSetService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListRuleSetRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }


    [Fact]
    public async Task ValidateAsync_InvalidPagedRequest_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleSetRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleSetService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleSetRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidPagedRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListRuleSetRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleSetService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleSetRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPage_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleSetRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 5,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleSetService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleSetRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}