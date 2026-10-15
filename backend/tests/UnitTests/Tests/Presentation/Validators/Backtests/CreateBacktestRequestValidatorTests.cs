using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace UnitTests.Tests.Presentation.Validators.BackTests;

public class CreateBackTestRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IRuleVersionService> _mockRuleVersionService;
    private readonly Mock<IBackTestService> _mockBackTestService;
    private readonly CreateBackTestRequestValidator _sut;

    public CreateBackTestRequestValidatorTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockRuleVersionService = new Mock<IRuleVersionService>();
        _mockBackTestService = new Mock<IBackTestService>();
        _sut = new CreateBackTestRequestValidator(_mockRuleService.Object, _mockRuleVersionService.Object, _mockBackTestService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture) with
        {
            RuleId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_RuleDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Rule not found");
    }

    [Fact]
    public async Task ValidateAsync_ActiveBackTestExists_FailsValidation()
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockBackTestService
            .Setup(x => x.ExistsActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithErrorMessage("An active backTest already exists for this rule");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task ValidateAsync_SampleSizeOutOfRange_FailsValidation(int sampleSize)
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture) with
        {
            Body = FakeCreateBackTestRequestBodyDto.CreateValid(Fixture) with
            {
                SampleSize = sampleSize
            }
        };

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockBackTestService
            .Setup(x => x.ExistsActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body!.SampleSize)
            .WithErrorMessage("Sample size must be between 0 and 100");
    }

    [Fact]
    public async Task ValidateAsync_VersionNumberProvidedbutDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture) with
        {
            Body = FakeCreateBackTestRequestBodyDto.CreateValid(Fixture) with
            {
                VersionNumber = 2
            }
        };

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockBackTestService
            .Setup(x => x.ExistsActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _mockRuleVersionService
            .Setup(x => x.VersionExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.Errors.Should().Contain(x =>
            x.ErrorCode == HttpStatusCode.NotFound.ToString() &&
            x.ErrorMessage == "Rule version not found");
    }

    [Fact]
    public async Task ValidateAsync_VersionNumberProvidedAndExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture) with
        {
            Body = FakeCreateBackTestRequestBodyDto.CreateValid(Fixture) with
            {
                SampleSize = 50,
                VersionNumber = 2
            }
        };

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockBackTestService
            .Setup(x => x.ExistsActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _mockRuleVersionService
            .Setup(x => x.VersionExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation(int sampleSize)
    {
        // Arrange
        var request = FakeCreateBackTestRequestDto.CreateValid(Fixture) with
        {
            Body = FakeCreateBackTestRequestBodyDto.CreateValid(Fixture) with
            {
                SampleSize = sampleSize
            }
        };

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockBackTestService
            .Setup(x => x.ExistsActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
