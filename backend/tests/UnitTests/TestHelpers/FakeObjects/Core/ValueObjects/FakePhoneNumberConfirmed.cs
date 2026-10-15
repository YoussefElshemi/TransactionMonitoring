using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePhoneNumberConfirmed
{
    public static PhoneNumberConfirmed CreateValid(IFixture fixture)
    {
        return new PhoneNumberConfirmed(fixture.Create<bool>());
    }
}