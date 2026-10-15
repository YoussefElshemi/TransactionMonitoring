using AutoFixture;
using Core.Enums;
using Core.Models.Invitations;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Invitations;

public static class FakeInvitation
{
    public static Invitation CreateValid(IFixture fixture)
    {
        return new Invitation
        {
            InvitationId = new InvitationId(fixture.Create<Guid>()),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Email = FakeEmail.CreateValid(fixture),
            UserRole = UserRole.User,
            SubOrganizationId = null,
            InvitedByUserId = FakeApplicationUserId.CreateValid(fixture),
            ExpiresAt = new ExpiresAt(TestTimeConstants.Today.AddDays(14)),
            AcceptedAt = null,
            AcceptedByUserId = null,
            RevokedAt = null,
            RevokedByUserId = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
