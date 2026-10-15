using Core.Interfaces.Services;
using Core.Models.BackTests;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace UnitTests.Tests.Presentation.Validators.BackTests;

public class ListBackTestResultRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IBackTestService> _mockBackTestService;
    private readonly Mock<IBackTestResultService> _mockBackTestResultService;
    private readonly ListBackTestResultRequestValidator _sut;

    public ListBackTestResultRequestValidatorTests()
    {
        _mockBackTestService = new Mock<IBackTestService>();
        _mockBackTestResultService = new Mock<IBackTestResultService>();
        _sut = new ListBackTestResultRequestValidator(_mockBackTestService.Object, _mockBackTestResultService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListBackTestResultRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockBackTestService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));
        _mockBackTestResultService
            .Setup(x => x.GetCountAsync(It.IsAny<ListBackTestResultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_InvalidPagedRequest_FailsValidation()
    {
        // Arrange
        var request = FakeListBackTestResultRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockBackTestService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));
        _mockBackTestResultService
            .Setup(x => x.GetCountAsync(It.IsAny<ListBackTestResultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPage_FailsValidation()
    {
        // Arrange
        var request = FakeListBackTestResultRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 5,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockBackTestService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));
        _mockBackTestResultService
            .Setup(x => x.GetCountAsync(It.IsAny<ListBackTestResultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}
