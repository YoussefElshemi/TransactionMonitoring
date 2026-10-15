using AutoFixture;
using Core.Enums;
using Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;

public static class FakeCreateOrganizationRequest
{
    public static CreateOrganizationRequest CreateValid(IFixture fixture)
    {
        return new CreateOrganizationRequest
        {
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            Status = fixture.Create<OrganizationStatus>(),
            HoldWindow = null,
            HoldExpiryVerdict = null,
            DeduplicationWindow = null,
            CaseGroupingPeriod = null
        };
    }
}