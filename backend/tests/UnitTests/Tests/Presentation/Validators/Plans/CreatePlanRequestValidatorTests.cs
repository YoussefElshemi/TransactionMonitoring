using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Validators.Plans;

public class CreatePlanRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly CreatePlanRequestValidator _sut;

    public CreatePlanRequestValidatorTests()
    {
        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockPlanService
            .Setup(x => x.ExistsByNameAsync(It.IsAny<PlanName>(), It.IsAny<OrganizationId?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _sut = new CreatePlanRequestValidator(
            _mockOrganizationService.Object, _mockPlanService.Object, new PlanPriceRequestValidator());
    }

    [Fact]
    public async Task ValidateAsync_NameIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Name = string.Empty } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Name)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_NameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Name = new string('a', PlanName.MaxLength + 1) } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Name)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_NameIsAlreadyTakenForTheOwner_FailsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);

        _mockPlanService
            .Setup(x => x.ExistsByNameAsync(It.IsAny<PlanName>(), It.IsAny<OrganizationId?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Name)
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithErrorMessage("A plan with that name already exists");
    }

    [Fact]
    public async Task ValidateAsync_GivenAnOwner_ChecksUniquenessAgainstThatOwner()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        var organizationId = new OrganizationId(request.Body.OrganizationId!.Value);

        // Act
        await _sut.TestValidateAsync(request);

        // Assert
        _mockPlanService.Verify(
            x => x.ExistsByNameAsync(It.IsAny<PlanName>(), organizationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_GivenNoOwner_ChecksUniquenessAgainstTheCatalogue()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { OrganizationId = null } };

        // Act
        await _sut.TestValidateAsync(request);

        // Assert
        _mockPlanService.Verify(
            x => x.ExistsByNameAsync(It.IsAny<PlanName>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_DescriptionExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Description = new string('a', PlanDescription.MaxLength + 1) } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Description)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.OrganizationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Organization not found");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIsNull_DoesNotLookItUp()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { OrganizationId = null } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.OrganizationId);

        _mockOrganizationService.Verify(
            x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_CostPerRequestIsNegative_FailsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { CostPerRequest = -0.01m } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.CostPerRequest)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_CostPerRequestIsZero_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { CostPerRequest = 0 } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_GivenAValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreatePlanRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
