using Core.Configs;
using Core.Helpers;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class AuditAnchorSignerTests : BaseTestClass
{
    private static readonly byte[] MasterKey = DataKeyWrapper.ReadMasterKey(PayloadProtectionConfig.DevelopmentMasterKey);

    [Fact]
    public void Sign_VerifiesTheAnchorItWasTakenOver()
    {
        // Arrange
        var auditAnchor = Anchor();

        // Act
        var verified = AuditAnchorSigner.Verify(MasterKey, auditAnchor);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_RejectsAnAnchorMovedToADifferentPositionOrDigest()
    {
        // Arrange
        var auditAnchor = Anchor();

        // Act
        var moved = AuditAnchorSigner.Verify(MasterKey, auditAnchor with { Sequence = new Sequence(999) });
        var redigested = AuditAnchorSigner.Verify(
            MasterKey, auditAnchor with { Hash = new Hash(new string('b', Hash.Length)) });
        var backdated = AuditAnchorSigner.Verify(
            MasterKey, auditAnchor with { CreatedAt = new CreatedAt(DateTimeOffset.UnixEpoch) });

        // Assert
        moved.Should().BeFalse();
        redigested.Should().BeFalse();
        backdated.Should().BeFalse();
    }

    [Fact]
    public void Verify_RejectsAnAnchorSignedUnderAnotherInstallationsKey()
    {
        // Arrange
        var otherMasterKey = DataKeyWrapper.Create();
        var auditAnchor = Anchor();

        // Act
        var verified = AuditAnchorSigner.Verify(otherMasterKey, auditAnchor);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Sign_DoesNotProduceAPlainHmacUnderTheMasterKey()
    {
        // Arrange
        var auditAnchor = Anchor();

        // Act
        var signature = AuditAnchorSigner.Sign(
            MasterKey, auditAnchor.AuditAnchorId, auditAnchor.OrganizationId, auditAnchor.Sequence,
            auditAnchor.Hash, auditAnchor.CreatedAt);

        // Assert
        signature.ToString().Should().HaveLength(64);
        signature.ToString().Should().MatchRegex("^[0-9a-f]+$");
    }

    private static AuditAnchor Anchor()
    {
        var auditAnchorId = new AuditAnchorId(new Guid("88888888-8888-8888-8888-888888888888"));
        var organizationId = new OrganizationId(new Guid("33333333-3333-3333-3333-333333333333"));
        var sequence = new Sequence(42);
        var hash = new Hash(new string('a', Hash.Length));
        var createdAt = new CreatedAt(new DateTimeOffset(2026, 8, 28, 2, 0, 0, TimeSpan.Zero));

        return new AuditAnchor
        {
            AuditAnchorId = auditAnchorId,
            OrganizationId = organizationId,
            Sequence = sequence,
            Hash = hash,
            Signature = AuditAnchorSigner.Sign(MasterKey, auditAnchorId, organizationId, sequence, hash, createdAt),
            CreatedAt = createdAt
        };
    }
}
