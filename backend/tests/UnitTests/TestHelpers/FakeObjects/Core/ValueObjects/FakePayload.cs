using AutoFixture;
using Core.Extensions;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePayload
{
    public static Payload CreateValid(IFixture fixture)
    {
        return new Payload(FakeJsonObject.CreateValid(fixture).Serialize().ToBase64String());
    }
}