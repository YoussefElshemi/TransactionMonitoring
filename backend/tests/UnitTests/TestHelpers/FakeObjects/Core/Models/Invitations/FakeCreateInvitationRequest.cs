using AutoFixture;
using Core.Enums;
using Core.Models.Invitations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Invitations;

public static class FakeCreateInvitationRequest
{
    public static CreateInvitationRequest CreateValid(IFixture fixture)
    {
        return new CreateInvitationRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Email = FakeEmail.CreateValid(fixture),
            UserRole = UserRole.User,
            SubOrganizationId = null,
            InvitedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
