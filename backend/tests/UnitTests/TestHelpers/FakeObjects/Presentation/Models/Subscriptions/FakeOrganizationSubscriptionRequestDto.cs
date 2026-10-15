using AutoFixture;
using Presentation.Models.Subscriptions;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

public static class FakeOrganizationSubscriptionRequestDto
{
    public static OrganizationSubscriptionRequestDto CreateValid(IFixture fixture)
    {
        return new OrganizationSubscriptionRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            SubscriptionId = fixture.Create<Guid>()
        };
    }
}
