using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTransactionSchemaId
{
    public static TransactionSchemaId CreateValid(IFixture fixture)
    {
        return new TransactionSchemaId(fixture.Create<Guid>());
    }
}
