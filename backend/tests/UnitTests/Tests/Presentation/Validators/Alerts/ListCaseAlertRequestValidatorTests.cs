using System.Net;
using Core.Interfaces.Services;
using Core.Models.Alerts;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Alerts;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Alerts;

namespace UnitTests.Tests.Presentation.Validators.Alerts;

public class ListCaseAlertRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IAlertService> _mockAlertService = new();
    private readonly Mock<ICaseService> _mockCaseService = new();
    private readonly ListCaseAlertRequestValidator _sut;

    public ListCaseAlertRequestValidatorTests()
    {
        _mockAlertService
            .Setup(x => x.GetCountAsync(It.IsAny<ListAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new ListCaseAlertRequestValidator(_mockAlertService.Object, _mockCaseService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_KnownCase_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseAlertRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RouteCaseId);
    }

    [Fact]
    public async Task ValidateAsync_UnknownCase_FailsWithNotFound()
    {
        // Arrange
        var request = FakeListCaseAlertRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RouteCaseId)
            .WithErrorCode(nameof(HttpStatusCode.NotFound))
            .WithErrorMessage("Case not found");
    }

    [Fact]
    public async Task ValidateAsync_EmptyRouteCaseId_FailsWithoutProbingTheService()
    {
        // Arrange
        var request = FakeListCaseAlertRequestDto.CreateValid(Fixture) with
        {
            RouteCaseId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RouteCaseId);

        _mockCaseService.Verify(
            x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ProbesTheCaseWithinTheCallersOrganization()
    {
        // Arrange
        var request = FakeListCaseAlertRequestDto.CreateValid(Fixture);

        // Act
        await _sut.TestValidateAsync(request);

        // Assert
        _mockCaseService.Verify(
            x => x.ExistsAsync(
                new OrganizationId(request.OrganizationId),
                new CaseId(request.RouteCaseId),
                new IncludeDeleted(false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_InvalidStatus_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseAlertRequestDto.CreateValid(Fixture) with
        {
            Status = "Invalid"
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorCode("StringEnumValidator");
    }

    [Fact]
    public void Validator_IsRegisteredAgainstItsOwnRequestType()
    {
        // Arrange
        var listAlertRequestValidatorType = typeof(ListAlertRequestValidator).BaseType;
        var listCaseAlertRequestValidatorType = typeof(ListCaseAlertRequestValidator).BaseType;

        // Act
        var sameValidatedType = listAlertRequestValidatorType == listCaseAlertRequestValidatorType;

        // Assert
        sameValidatedType.Should().BeFalse();
    }
}
