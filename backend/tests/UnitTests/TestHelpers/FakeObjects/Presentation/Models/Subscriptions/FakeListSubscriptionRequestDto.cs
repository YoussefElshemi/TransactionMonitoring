using AutoFixture;
using Presentation.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

public static class FakeListSubscriptionRequestDto
{
    public static ListSubscriptionRequestDto CreateValid(IFixture fixture)
    {
        return new ListSubscriptionRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            IncludeSuperseded = false,
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
