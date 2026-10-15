using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeContactName
{
    public static ContactName CreateValid(IFixture fixture)
    {
        return new ContactName(fixture.Create<string>());
    }
}
