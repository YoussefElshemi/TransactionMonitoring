using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class TaxIdentifierValidatorTests : BaseTestClass
{
    [Theory]
    [InlineData("GB", "GB123456789")]
    [InlineData("IE", "IE1234567FA")]
    [InlineData("DE", "DE123456789")]
    [InlineData("NL", "NL123456789B01")]
    public void IsWellFormed_GivenARegistrationInItsOwnCountrySShape_Accepts(string countryCode, string taxIdentifier)
    {
        // Arrange
        // Act
        var wellFormed = TaxIdentifierValidator.IsWellFormed(new CountryCode(countryCode), new TaxIdentifier(taxIdentifier));

        // Assert
        wellFormed.Should().BeTrue();
    }

    [Theory]
    [InlineData("GB", "GB12345")]
    [InlineData("DE", "DE12345678901234")]
    [InlineData("IE", "IE12")]
    public void IsWellFormed_GivenANumberThatIsNotItsCountrySShape_Refuses(string countryCode, string taxIdentifier)
    {
        // Arrange
        // Act
        var wellFormed = TaxIdentifierValidator.IsWellFormed(new CountryCode(countryCode), new TaxIdentifier(taxIdentifier));

        // Assert
        wellFormed.Should().BeFalse();
    }

    [Fact]
    public void IsWellFormed_GivenAnotherCountrySRegistration_Refuses()
    {
        // Arrange
        // Act
        var wellFormed = TaxIdentifierValidator.IsWellFormed(new CountryCode("DE"), new TaxIdentifier("GB123456789"));

        // Assert
        wellFormed.Should().BeFalse();
    }

    [Fact]
    public void IsWellFormed_GivenACountryWithNoRulesOnFile_Refuses()
    {
        // Arrange
        // Act
        var wellFormed = TaxIdentifierValidator.IsWellFormed(new CountryCode("US"), new TaxIdentifier("123456789"));

        // Assert
        wellFormed.Should().BeFalse();
    }

    [Theory]
    [InlineData("GB", "123456789", "GB123456789")]
    [InlineData("GB", "GB 123 456 789", "GB123456789")]
    [InlineData("GB", "gb123456789", "GB123456789")]
    public void Normalise_GivenARegistrationWrittenAnyWay_ReachesOneForm(string countryCode, string written, string expected)
    {
        // Arrange
        var country = new CountryCode(countryCode);

        // Act
        var normalised = TaxIdentifierValidator.Normalise(country, new TaxIdentifier(written));

        // Assert
        ((string)normalised).Should().Be(expected);
        TaxIdentifierValidator.IsWellFormed(country, normalised).Should().BeTrue();
    }

    [Fact]
    public void Normalise_GivenAGreekRegistration_UsesThePrefixTheRegisterUses()
    {
        // Arrange
        var country = new CountryCode("GR");

        // Act
        var normalised = TaxIdentifierValidator.Normalise(country, new TaxIdentifier("123456789"));

        // Assert
        ((string)normalised).Should().Be("EL123456789");
        TaxIdentifierValidator.IsWellFormed(country, normalised).Should().BeTrue();
    }
}
