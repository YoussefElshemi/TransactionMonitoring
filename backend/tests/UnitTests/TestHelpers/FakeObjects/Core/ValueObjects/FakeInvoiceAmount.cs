using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceAmount
{
    public static InvoiceAmount CreateValid(IFixture fixture)
    {
        return new InvoiceAmount(fixture.Create<decimal>());
    }
}