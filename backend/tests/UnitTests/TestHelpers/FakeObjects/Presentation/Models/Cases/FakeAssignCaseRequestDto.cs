using AutoFixture;
using Presentation.Models.Cases;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeAssignCaseRequestDto
{
    public static AssignCaseRequestDto CreateValid(IFixture fixture)
    {
        var userId = fixture.Create<Guid>();

        return new AssignCaseRequestDto
        {
            CaseId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            AssignedByUserId = userId,
            Body = new AssignCaseRequestBodyDto
            {
                UserId = userId
            }
        };
    }
}
