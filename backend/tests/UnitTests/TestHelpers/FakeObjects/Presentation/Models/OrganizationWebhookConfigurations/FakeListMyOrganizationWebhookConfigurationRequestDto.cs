using AutoFixture;
using Presentation.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

public static class FakeListMyOrganizationWebhookConfigurationRequestDto
{
    public static ListMyOrganizationWebhookConfigurationRequestDto CreateValid(IFixture fixture)
    {
        return new ListMyOrganizationWebhookConfigurationRequestDto
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
