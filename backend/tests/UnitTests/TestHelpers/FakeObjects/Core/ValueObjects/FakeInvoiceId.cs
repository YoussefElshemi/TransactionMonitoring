using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceId
{
    public static InvoiceId CreateValid(IFixture fixture)
    {
        return new InvoiceId(fixture.Create<Guid>());
    }
}