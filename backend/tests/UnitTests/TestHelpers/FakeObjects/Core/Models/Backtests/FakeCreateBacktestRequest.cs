using AutoFixture;
using Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;

public static class FakeCreateBackTestRequest
{
    public static CreateBackTestRequest CreateValid(IFixture fixture)
    {
        return new CreateBackTestRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            VersionNumber = null,
            SampleSize = FakeSampleSize.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
