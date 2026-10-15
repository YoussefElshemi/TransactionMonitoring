using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTransactionId
{
    public static TransactionId CreateValid(IFixture fixture)
    {
        return new TransactionId(fixture.Create<Guid>());
    }
}
