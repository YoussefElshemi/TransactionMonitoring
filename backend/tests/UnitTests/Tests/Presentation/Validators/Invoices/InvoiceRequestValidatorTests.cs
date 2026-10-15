using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Invoices;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Invoices;

namespace UnitTests.Tests.Presentation.Validators.Invoices;

public class InvoiceRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IInvoiceService> _mockInvoiceService;
    private readonly InvoiceRequestValidator _sut;

    public InvoiceRequestValidatorTests()
    {
        _mockInvoiceService = new Mock<IInvoiceService>();
        _sut = new InvoiceRequestValidator(_mockInvoiceService.Object);
    }

    [Fact]
    public async Task ValidateAsync_InvoiceIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeInvoiceRequestDto.CreateValid(Fixture) with
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
        var request = FakeInvoiceRequestDto.CreateValid(Fixture);

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
        var request = FakeInvoiceRequestDto.CreateValid(Fixture);

        _mockInvoiceService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<InvoiceId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.InvoiceId);
    }
}