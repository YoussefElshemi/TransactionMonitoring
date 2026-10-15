using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Validators.Cases;

public class RecoverCaseRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly RecoverCaseRequestValidator _sut;

    public RecoverCaseRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _sut = new RecoverCaseRequestValidator(_mockCaseService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverCaseRequestDto.CreateValid(Fixture) with
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
        var request = FakeRecoverCaseRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeRecoverCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));


        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }
}