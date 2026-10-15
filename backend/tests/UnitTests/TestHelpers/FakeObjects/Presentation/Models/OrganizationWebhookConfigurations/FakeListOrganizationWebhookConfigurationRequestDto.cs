using AutoFixture;
using Presentation.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

public static class FakeListOrganizationWebhookConfigurationRequestDto
{
    public static ListOrganizationWebhookConfigurationRequestDto CreateValid(IFixture fixture)
    {
        return new ListOrganizationWebhookConfigurationRequestDto
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