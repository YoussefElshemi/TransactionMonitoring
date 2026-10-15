using Core.Interfaces.Services;
using Core.Models.CaseNotes;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Validators.CaseNotes;

public class ListCaseNoteRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseNoteService> _mockCaseNoteService;
    private readonly ListCaseNoteRequestValidator _sut;

    public ListCaseNoteRequestValidatorTests()
    {
        _mockCaseNoteService = new Mock<ICaseNoteService>();
        _sut = new ListCaseNoteRequestValidator(_mockCaseNoteService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPageCount_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseNoteRequestDto.CreateValid(Fixture) with
        {
            PageSize = 1,
            PageNumber = 2
        };

        _mockCaseNoteService
            .Setup(x => x.GetCountAsync(It.IsAny<ListCaseNoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(1));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_PageSizeExceedsMax_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseNoteRequestDto.CreateValid(Fixture) with
        {
            PageSize = 200
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateInFuture_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseNoteRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateAfterToDate_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseNoteRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseNoteRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10
        };

        _mockCaseNoteService
            .Setup(x => x.GetCountAsync(It.IsAny<ListCaseNoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }
}