using System.Text.Json.Nodes;
using AutoFixture;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeJsonObject
{
    public static JsonObject CreateValid(IFixture fixture)
    {
        return new JsonObject
        {
            ["Key"] = "Value"
        };
    }
}