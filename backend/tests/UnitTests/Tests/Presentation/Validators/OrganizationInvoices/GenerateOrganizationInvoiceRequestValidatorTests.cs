using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationInvoices;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

namespace UnitTests.Tests.Presentation.Validators.OrganizationInvoices;

public class GenerateOrganizationInvoiceRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _organizationServiceMock = new();
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly GenerateOrganizationInvoiceRequestValidator _sut;

    public GenerateOrganizationInvoiceRequestValidatorTests()
    {
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _sut = new GenerateOrganizationInvoiceRequestValidator(
            _organizationServiceMock.Object,
            _invoiceServiceMock.Object,
            MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Month = 12,
                Year = MockTimeProvider.GetUtcNow().Year - 1
            }
        };

        _organizationServiceMock
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _invoiceServiceMock
            .Setup(x => x.ExistsByDateAsync(It.IsAny<OrganizationId>(), It.IsAny<Month>(), It.IsAny<Year>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with
        {
            OrganizationId = Guid.Empty,
            Body = request.Body with
            {
                Month = 12,
                Year = MockTimeProvider.GetUtcNow().Year - 1
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Month = 12,
                Year = MockTimeProvider.GetUtcNow().Year - 1
            }
        };

        _organizationServiceMock
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
    public async Task ValidateAsync_InvalidMonth_FailsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Month = 13,
                Year = MockTimeProvider.GetUtcNow().Year
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Month)
            .WithErrorMessage("Month must be between 1 and 12");
    }

    [Fact]
    public async Task ValidateAsync_InvalidYear_FailsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Month = 1,
                Year = MockTimeProvider.GetUtcNow().Year + 1
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Year)
            .WithErrorMessage("Year must be between 1 and this year");
    }

    [Fact]
    public async Task ValidateAsync_DateNotInPast_FailsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with {
            Body = request.Body with
            {
                Month = MockTimeProvider.GetUtcNow().Month, Year = MockTimeProvider.GetUtcNow().Year
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Time period provided must be in the past, and the month has ended");
    }

    [Fact]
    public async Task ValidateAsync_InvoiceAlreadyExists_FailsValidation()
    {
        // Arrange
        var request = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Month = 12,
                Year = MockTimeProvider.GetUtcNow().Year - 1
            }
        };

        _invoiceServiceMock
            .Setup(x => x.ExistsByDateAsync(It.IsAny<OrganizationId>(), It.IsAny<Month>(), It.IsAny<Year>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Invoice already exists for this time period");
    }
}