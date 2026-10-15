using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class CardNumberDetectorTests : BaseTestClass
{
    [Theory]
    [InlineData("4111111111111111")]      // Visa
    [InlineData("4012888888881881")]      // Visa
    [InlineData("5555555555554444")]      // Mastercard
    [InlineData("2223003122003222")]      // Mastercard, expanded range
    [InlineData("378282246310005")]       // American Express
    [InlineData("6011111111111117")]      // Discover
    [InlineData("3530111333300000")]      // JCB
    public void IsCardNumber_RecognisesARealCardNumber(string value)
    {
        // Act
        CardNumberDetector.IsCardNumber(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("4111-1111-1111-1111")]
    public void IsCardNumber_RecognisesACardNumberAsPeopleTypeIt(string value)
    {
        // Act & Assert
        CardNumberDetector.IsCardNumber(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("4111111111111112")]      // right shape and prefix, fails Luhn
    [InlineData("1234567890123456")]      // passes nothing
    [InlineData("411111111111")]          // too short
    [InlineData("41111111111111111111")]  // too long
    [InlineData("GB29NWBK60161331926819")] // IBAN
    [InlineData("")]
    [InlineData(null)]
    public void IsCardNumber_LeavesAnythingElseAlone(string? value)
    {
        // Act
        CardNumberDetector.IsCardNumber(value).Should().BeFalse();
    }

    [Fact]
    public void IsCardNumber_RefusesALuhnValidNumberWithNoIssuerPrefix()
    {
        // Arrange
        const string accountIdentifier = "9999999999999995";

        // Act & Assert
        CardNumberDetector.IsCardNumber(accountIdentifier).Should().BeFalse();
    }

    [Fact]
    public void LastFour_TakesTheDigitsAReceiptWouldPrint()
    {
        // Arrange
        CardNumberDetector.TryNormalise("4111 1111 1111 1111", out var digits).Should().BeTrue();

        // Act
        CardNumberDetector.LastFour(digits).Should().Be("1111");
    }
}
