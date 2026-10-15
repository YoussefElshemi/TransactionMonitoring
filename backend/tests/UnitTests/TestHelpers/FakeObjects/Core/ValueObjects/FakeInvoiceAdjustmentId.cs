using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceAdjustmentId
{
    public static InvoiceAdjustmentId CreateValid(IFixture fixture)
    {
        return new InvoiceAdjustmentId(fixture.Create<Guid>());
    }
}
