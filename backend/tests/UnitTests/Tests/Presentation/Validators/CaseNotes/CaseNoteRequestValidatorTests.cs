using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Validators.CaseNotes;

public class CaseNoteRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseNoteService> _mockCaseNoteService;
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly CaseNoteRequestValidator _sut;

    public CaseNoteRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _mockCaseNoteService = new Mock<ICaseNoteService>();
        _sut = new CaseNoteRequestValidator(_mockCaseService.Object, _mockCaseNoteService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCaseNoteRequestDto.CreateValid(Fixture) with
        {
            CaseId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_CaseDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCaseNoteRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Case not found");
    }

    [Fact]
    public async Task ValidateAsync_CaseExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeCaseNoteRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }

    [Fact]
    public async Task ValidateAsync_CaseNoteIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCaseNoteRequestDto.CreateValid(Fixture) with
        {
            CaseNoteId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseNoteId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_CaseNoteDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCaseNoteRequestDto.CreateValid(Fixture);

        _mockCaseNoteService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<CaseNoteId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseNoteId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Case Note not found");
    }

    [Fact]
    public async Task ValidateAsync_CaseNoteExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeCaseNoteRequestDto.CreateValid(Fixture);

        _mockCaseNoteService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<CaseNoteId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseNoteId);
    }
}