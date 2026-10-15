using AutoFixture;
using Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;

public static class FakeGenerateInvoiceRequest
{
    public static GenerateInvoiceRequest CreateValid(IFixture fixture)
    {
        return new GenerateInvoiceRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Month = FakeMonth.CreateValid(fixture),
            Year = FakeYear.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}