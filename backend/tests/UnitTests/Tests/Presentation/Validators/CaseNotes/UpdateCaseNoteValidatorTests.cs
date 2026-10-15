using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Validators.CaseNotes;

public class UpdateCaseNoteValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseNoteService> _mockCaseNoteService;
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly UpdateCaseNoteValidator _sut;

    public UpdateCaseNoteValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _mockCaseNoteService = new Mock<ICaseNoteService>();
        _sut = new UpdateCaseNoteValidator(_mockCaseService.Object, _mockCaseNoteService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseNoteIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture) with
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
    public async Task ValidateAsync_CaseNoteIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_CaseIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture) with
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
    public async Task ValidateAsync_CaseIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_MessageExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Message = new string('A', 2049)
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Message)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateCaseNoteRequestDto.CreateValid(Fixture);

        _mockCaseNoteService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<CaseNoteId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
        result.ShouldNotHaveValidationErrorFor(x => x.CaseNoteId);
        result.ShouldNotHaveValidationErrorFor(x => x.Body.Message);
    }
}