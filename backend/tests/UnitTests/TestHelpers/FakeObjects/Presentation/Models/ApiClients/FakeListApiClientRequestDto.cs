using AutoFixture;
using Presentation.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeListApiClientRequestDto
{
    public static ListApiClientRequestDto CreateValid(IFixture fixture)
    {
        return new ListApiClientRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            FriendlyName = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
