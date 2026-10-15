using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.CaseNotes;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace UnitTests.Tests.Presentation.Validators.CaseNotes;

public class CreateCaseNoteRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly CreateCaseNoteRequestValidator _sut;

    public CreateCaseNoteRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new CreateCaseNoteRequestValidator(_mockCaseService.Object, _mockOrganizationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture) with
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
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);

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
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture) with
        {
            OrganizationId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Organization not found");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public async Task ValidateAsync_MessageIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Message = string.Empty
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Message)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_MessageExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);
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
    public async Task ValidateAsync_MessageIsValid_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateCaseNoteRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Message = "Valid message"
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.Message);
    }
}