using AutoFixture;
using Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeListApiClientRequest
{
    public static ListApiClientRequest CreateValid(IFixture fixture)
    {
        return new ListApiClientRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            FriendlyName = null,
            PageNumber = FakePageNumber.CreateValid(fixture),
            PageSize = FakePageSize.CreateValid(fixture),
            FromDate = FakeDateRange.CreateValid(fixture),
            ToDate = FakeDateRange.CreateValid(fixture)
        };
    }
}
