using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeCancelDeploymentRequestDto
{
    public static CancelDeploymentRequestDto CreateValid(IFixture fixture)
    {
        return new CancelDeploymentRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            DeploymentId = fixture.Create<Guid>()
        };
    }
}
