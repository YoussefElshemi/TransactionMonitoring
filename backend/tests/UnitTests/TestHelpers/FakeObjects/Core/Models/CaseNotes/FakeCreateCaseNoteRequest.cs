using AutoFixture;
using Core.Models.CaseNotes;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.CaseNotes;

public static class FakeCreateCaseNoteRequest
{
    public static CreateCaseNoteRequest CreateValid(IFixture fixture)
    {
        return new CreateCaseNoteRequest
        {
            CaseId = FakeCaseId.CreateValid(fixture),
            Message = FakeMessage.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture)
        };
    }
}