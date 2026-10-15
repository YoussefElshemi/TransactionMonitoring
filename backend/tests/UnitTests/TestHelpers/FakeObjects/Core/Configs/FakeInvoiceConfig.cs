using AutoFixture;
using Core.Configs;

namespace UnitTests.TestHelpers.FakeObjects.Core.Configs;

public static class FakeInvoiceConfig
{
    public static InvoiceConfig CreateValid(IFixture fixture)
    {
        return new InvoiceConfig
        {
            DaysUntilDue = fixture.Create<int>(),
            CompanyName = fixture.Create<string>()
        };
    }
}