using AutoFixture;
using Presentation.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Invoices;

public static class FakeListInvoiceRequestDto
{
    public static ListInvoiceRequestDto CreateValid(IFixture fixture)
    {
        return new ListInvoiceRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            OrganizationId = fixture.Create<Guid>()
        };
    }
}