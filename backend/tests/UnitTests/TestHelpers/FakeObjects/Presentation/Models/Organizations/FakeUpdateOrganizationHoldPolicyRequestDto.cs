using AutoFixture;
using Core.Enums;
using Core.Models.Common;
using Presentation.Models.Organizations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

public static class FakeUpdateOrganizationHoldPolicyRequestDto
{
    public static UpdateOrganizationHoldPolicyRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateOrganizationHoldPolicyRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateOrganizationHoldPolicyRequestBodyDto
            {
                HoldWindow = Patch<int?>.Set(fixture.Create<int>() % 1440 + 1),
                HoldExpiryVerdict = Patch<string?>.Set(fixture.Create<HoldExpiryVerdict>().ToString())
            }
        };
    }

    public static UpdateOrganizationHoldPolicyRequestDto CreateInvalid(IFixture fixture)
    {
        return new UpdateOrganizationHoldPolicyRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new UpdateOrganizationHoldPolicyRequestBodyDto
            {
                HoldWindow = Patch<int?>.Set(0),
                HoldExpiryVerdict = Patch<string?>.Set(fixture.Create<string>())
            }
        };
    }
}
