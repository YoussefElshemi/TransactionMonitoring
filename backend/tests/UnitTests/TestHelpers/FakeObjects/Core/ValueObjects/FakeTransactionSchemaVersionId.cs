using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTransactionSchemaVersionId
{
    public static TransactionSchemaVersionId CreateValid(IFixture fixture)
    {
        return new TransactionSchemaVersionId(fixture.Create<Guid>());
    }
}
