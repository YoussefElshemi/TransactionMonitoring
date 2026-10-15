using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Alerts;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Alerts;

namespace UnitTests.Tests.Presentation.Validators.Alerts;

public class AlertRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IAlertService> _mockAlertService = new();
    private readonly AlertRequestValidator _sut;

    public AlertRequestValidatorTests()
    {
        _sut = new AlertRequestValidator(_mockAlertService.Object);
    }

    [Fact]
    public async Task ValidateAsync_KnownAlert_SucceedsValidation()
    {
        // Arrange
        var request = FakeAlertRequestDto.CreateValid(Fixture);

        _mockAlertService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<AlertId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AlertId);
    }

    [Fact]
    public async Task ValidateAsync_UnknownAlert_FailsWithNotFound()
    {
        // Arrange
        var request = FakeAlertRequestDto.CreateValid(Fixture);

        _mockAlertService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<AlertId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AlertId)
            .WithErrorCode(nameof(HttpStatusCode.NotFound))
            .WithErrorMessage("Alert not found");
    }

    [Fact]
    public async Task ValidateAsync_EmptyAlertId_FailsWithoutProbingTheService()
    {
        // Arrange
        var request = FakeAlertRequestDto.CreateValid(Fixture) with
        {
            AlertId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AlertId);

        _mockAlertService.Verify(
            x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<AlertId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ProbesWithinTheCallersOrganizationAndExcludesDeleted()
    {
        // Arrange
        var request = FakeAlertRequestDto.CreateValid(Fixture);

        _mockAlertService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<AlertId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        await _sut.TestValidateAsync(request);

        // Assert
        _mockAlertService.Verify(
            x => x.ExistsAsync(
                new OrganizationId(request.OrganizationId),
                new AlertId(request.AlertId),
                new IncludeDeleted(false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
