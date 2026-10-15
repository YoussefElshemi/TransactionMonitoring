using AutoFixture;
using Core.Models.Adjustments;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;

public static class FakeCreateInvoiceAdjustmentRequest
{
    public static CreateInvoiceAdjustmentRequest CreateValid(IFixture fixture)
    {
        return new CreateInvoiceAdjustmentRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Month = FakeMonth.CreateValid(fixture),
            Year = FakeYear.CreateValid(fixture),
            Description = FakeInvoiceAdjustmentDescription.CreateValid(fixture),
            Amount = FakeAdjustmentAmount.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
