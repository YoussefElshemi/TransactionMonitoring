using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceLineId
{
    public static InvoiceLineId CreateValid(IFixture fixture)
    {
        return new InvoiceLineId(fixture.Create<Guid>());
    }
}
