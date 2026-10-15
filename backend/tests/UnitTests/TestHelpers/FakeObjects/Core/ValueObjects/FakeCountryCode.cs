using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCountryCode
{
    public static CountryCode CreateValid(IFixture fixture)
    {
        return new CountryCode("GB");
    }
}
