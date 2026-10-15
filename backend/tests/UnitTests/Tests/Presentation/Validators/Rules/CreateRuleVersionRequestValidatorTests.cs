using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class CreateRuleVersionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly CreateRuleVersionRequestValidator _sut;

    public CreateRuleVersionRequestValidatorTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _sut = new CreateRuleVersionRequestValidator(_mockRuleService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleVersionRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_RuleBodyInvalid_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleVersionRequestDto.CreateInvalid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.RuleBody);
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateRuleVersionRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RuleId);
        result.ShouldNotHaveValidationErrorFor(x => x.Body.RuleBody);
    }
}
