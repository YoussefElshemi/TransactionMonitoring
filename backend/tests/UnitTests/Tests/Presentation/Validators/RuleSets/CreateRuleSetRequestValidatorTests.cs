using Core.Interfaces.Services;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Validators.RuleSets;

public class CreateRuleSetRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ISubOrganizationService> _mockSubOrganizationService = new();
    private readonly CreateRuleSetRequestValidator _sut;

    public CreateRuleSetRequestValidatorTests()
    {
        _sut = new CreateRuleSetRequestValidator(_mockSubOrganizationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleSetRequestDto.CreateValid(Fixture) with
        {
            FriendlyName = string.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FriendlyName)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeCreateRuleSetRequestDto.CreateValid(Fixture) with
        {
            FriendlyName = new string('A', 51)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FriendlyName)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateRuleSetRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FriendlyName);
        result.ShouldNotHaveValidationErrorFor(x => x.IsEnabled);
    }
}