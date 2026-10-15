using AutoFixture;
using Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;

public static class FakeDocumentParty
{
    public static DocumentParty CreateValid(IFixture fixture)
    {
        return new DocumentParty
        {
            LegalName = FakeLegalName.CreateValid(fixture),
            Address = FakePostalAddress.CreateValid(fixture),
            CountryCode = FakeCountryCode.CreateValid(fixture),
            TaxIdentifier = FakeTaxIdentifier.CreateValid(fixture),
            Email = FakeEmail.CreateValid(fixture)
        };
    }
}
