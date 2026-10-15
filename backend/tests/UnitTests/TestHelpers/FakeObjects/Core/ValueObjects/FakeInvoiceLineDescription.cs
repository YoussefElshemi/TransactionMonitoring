using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceLineDescription
{
    public static InvoiceLineDescription CreateValid(IFixture fixture)
    {
        return new InvoiceLineDescription(fixture.Create<string>());
    }
}
