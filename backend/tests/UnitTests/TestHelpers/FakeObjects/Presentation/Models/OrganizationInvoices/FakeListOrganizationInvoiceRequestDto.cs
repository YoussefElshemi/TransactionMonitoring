using AutoFixture;
using Presentation.Models.OrganizationInvoices;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

public static class FakeListOrganizationInvoiceRequestDto
{
    public static ListOrganizationInvoiceRequestDto CreateValid(IFixture fixture)
    {
        return new ListOrganizationInvoiceRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            OrganizationId = fixture.Create<Guid>()
        };
    }
}