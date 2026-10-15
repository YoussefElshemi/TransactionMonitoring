using FluentValidation.TestHelper;
using Presentation.Validators.Statistics;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Statistics;

namespace UnitTests.Tests.Presentation.Validators.Statistics;

public class StatisticRequestValidatorTests : BaseTestClass
{
    private readonly StatisticRequestValidator _sut;

    public StatisticRequestValidatorTests()
    {
        _sut = new StatisticRequestValidator(MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_FromDateIsInTheFuture_FailsValidation()
    {
        // Arrange
        var request = FakeStatisticRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorMessage("From Date cannot be in the future.");
    }

    [Fact]
    public async Task ValidateAsync_FromDateIsAfterToDate_FailsValidation()
    {
        // Arrange
        var request = FakeStatisticRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => new { x.FromDate, x.ToDate })
            .WithErrorMessage("From Date must be before or equal to To Date.");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeStatisticRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }
}