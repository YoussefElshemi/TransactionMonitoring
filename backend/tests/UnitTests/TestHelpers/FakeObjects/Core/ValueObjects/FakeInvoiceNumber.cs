using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceNumber
{
    public static InvoiceNumber CreateValid(IFixture fixture)
    {
        return new InvoiceNumber("INV-2026-000001");
    }
}
