using AutoFixture;
using Core.Helpers;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDataKey
{
    public static DataKey CreateValid(IFixture fixture)
    {
        return DataKeyWrapper.Create();
    }
}
