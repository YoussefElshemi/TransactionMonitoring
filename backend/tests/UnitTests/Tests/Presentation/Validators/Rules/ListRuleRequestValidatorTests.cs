using System.Net;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class ListRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IRuleSetService> _mockRuleSetService;
    private readonly ListRuleRequestValidator _sut;

    public ListRuleRequestValidatorTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockRuleSetService = new Mock<IRuleSetService>();
        _sut = new ListRuleRequestValidator(_mockRuleSetService.Object, _mockRuleService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_RuleSetIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleRequestDto.CreateValid(Fixture) with
        {
            RuleSetId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleSetId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleSetDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleRequestDto.CreateValid(Fixture);

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleSetId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Rule Set not found");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListRuleRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }


    [Fact]
    public async Task ValidateAsync_InvalidPagedRequest_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0, // Invalid: Less than 1
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidPagedRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPage_FailsValidation()
    {
        // Arrange
        var request = FakeListRuleRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 5,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockRuleSetService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleService
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}