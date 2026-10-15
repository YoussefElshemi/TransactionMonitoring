using FluentValidation.TestHelper;
using Presentation.Validators.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Validators.Organizations;

public class CreateOrganizationRequestValidatorTests : BaseTestClass
{
    private readonly CreateOrganizationRequestValidator _sut = new();

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateOrganizationRequestDto.CreateValid(Fixture)
            with
            {
                FriendlyName = "Valid Organization Name"
            };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyFriendlyName_FailsValidation()
    {
        // Arrange
        var request = FakeCreateOrganizationRequestDto.CreateValid(Fixture)
            with
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
    public async Task ValidateAsync_FriendlyNameTooLong_FailsValidation()
    {
        // Arrange
        var request = FakeCreateOrganizationRequestDto.CreateValid(Fixture) with
        {
            FriendlyName = new string('A', 51)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FriendlyName)
            .WithErrorCode("MaximumLengthValidator");
    }
}
