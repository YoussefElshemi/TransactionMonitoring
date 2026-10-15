using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeTransferRuleRequestDto
{
    public static TransferRuleRequestDto CreateValid(IFixture fixture)
    {
        return new TransferRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new TransferRuleRequestBodyDto
            {
                TargetRuleSetId = fixture.Create<Guid>()
            }
        };
    }
}