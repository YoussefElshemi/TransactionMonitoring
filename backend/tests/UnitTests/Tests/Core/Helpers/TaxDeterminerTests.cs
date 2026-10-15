using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;

namespace UnitTests.Tests.Core.Helpers;

public class TaxDeterminerTests : BaseTestClass
{
    private static readonly CountryCode UnitedKingdom = new("GB");
    private static readonly CountryCode Ireland = new("IE");
    private static readonly CountryCode Germany = new("DE");
    private static readonly CountryCode UnitedStates = new("US");

    private static readonly TaxIdentifier IrishRegistration = new("IE1234567FA");

    [Fact]
    public void Determine_GivenACustomerInTheSupplierSCountry_ChargesTheSupplierSRate()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(UnitedKingdom, UnitedKingdom, null, null, FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.Standard);
        determination.TaxRate.Should().Be(new TaxRate(20m));
    }

    [Fact]
    public void Determine_GivenADomesticCustomerWithARegistration_StillChargesTheSupplierSRate()
    {
        // Arrange
        var registration = new TaxIdentifier("GB123456789");

        // Act
        var determination = TaxDeterminer.Determine(
            UnitedKingdom, UnitedKingdom, registration, new VerifiedAt(MockTimeProvider.GetUtcNow()), FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.Standard);
        determination.TaxRate.Should().Be(new TaxRate(20m));
    }

    [Fact]
    public void Determine_GivenAnEuBusinessWithAConfirmedRegistration_ReverseCharges()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(
            UnitedKingdom, Ireland, IrishRegistration, new VerifiedAt(MockTimeProvider.GetUtcNow()), FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.ReverseCharge);
        determination.TaxRate.Should().Be(new TaxRate(0m));
        ((string)determination.Description).Should().Contain("Reverse charge");
    }

    [Fact]
    public void Determine_GivenAnEuCustomerWhoseRegistrationWasNeverConfirmed_DoesNotReverseCharge()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(UnitedKingdom, Ireland, IrishRegistration, null, FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().NotBe(TaxTreatment.ReverseCharge);
    }

    [Fact]
    public void Determine_GivenANonEuSupplierAndAnEuConsumer_IsOutOfScope()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(UnitedKingdom, Germany, null, null, FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.OutOfScope);
        determination.TaxRate.Should().Be(new TaxRate(0m));
    }

    [Fact]
    public void Determine_GivenAnEuSupplierAndAnEuConsumer_ChargesTheCustomerSRate()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(Ireland, Germany, null, null, FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.Standard);
        determination.TaxRate.Should().Be(new TaxRate(19m));
    }

    [Fact]
    public void Determine_GivenACustomerOutsideEveryJurisdictionTheSupplierIsIn_IsOutOfScope()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(UnitedKingdom, UnitedStates, null, null, FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.OutOfScope);
        determination.TaxRate.Should().Be(new TaxRate(0m));
    }

    [Fact]
    public void Determine_GivenASupplierInNoKnownJurisdiction_ChargesNothingAndSaysWhy()
    {
        // Arrange
        // Act
        var determination = TaxDeterminer.Determine(UnitedStates, UnitedKingdom, null, null, FakeDisplayPreferences.Translator());

        // Assert
        determination.TaxTreatment.Should().Be(TaxTreatment.OutOfScope);
        ((string)determination.Description).Should().Contain("no registered jurisdiction");
    }
}
