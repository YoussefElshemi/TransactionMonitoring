using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationInvoices;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

namespace UnitTests.Tests.Presentation.Validators.OrganizationInvoices;

public class OrganizationInvoiceRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly Mock<IInvoiceService> _mockInvoiceService;
    private readonly OrganizationInvoiceRequestValidator _sut;

    public OrganizationInvoiceRequestValidatorTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _mockInvoiceService = new Mock<IInvoiceService>();
        _sut = new OrganizationInvoiceRequestValidator(
            _mockOrganizationService.Object,
            _mockInvoiceService.Object);
    }

    [Fact]
    public async Task ValidateAsync_InvoiceIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeOrganizationInvoiceRequestDto.CreateValid(Fixture) with
        {
            InvoiceId = Guid.Empty
        };


        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_InvoiceIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeOrganizationInvoiceRequestDto.CreateValid(Fixture);

        _mockInvoiceService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<InvoiceId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Invoice not found");
    }

    [Fact]
    public async Task ValidateAsync_InvoiceIdDoesExist_SucceedsValidation()
    {
        // Arrange
        var request = FakeOrganizationInvoiceRequestDto.CreateValid(Fixture);

        _mockInvoiceService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<InvoiceId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.InvoiceId);
    }
}