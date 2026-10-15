using AutoFixture;
using Core.Enums;
using Presentation.Models.Cases;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeResolveCaseRequestDto
{
    public static ResolveCaseRequestDto CreateValid(IFixture fixture)
    {
        return new ResolveCaseRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            CaseId = fixture.Create<Guid>(),
            Body = new ResolveCaseRequestBodyDto
            {
                Status = CaseStatus.Approved.ToString(),
                Disposition = CaseDisposition.FalsePositive.ToString(),
                UserId = fixture.Create<Guid>()
            }
        };
    }
}