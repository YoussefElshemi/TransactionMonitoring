using Core.Enums;
using Core.Helpers;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

// The first test's expected value was taken from Postgres via the migration's trigger, so the two canonical forms can't drift.
public class AuditChainHasherTests : BaseTestClass
{
    private static readonly Guid AuditLogId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EntityId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OrganizationId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ChangedByUserId = new("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void Compute_MatchesTheDigestPostgresWritesForTheSameEntry()
    {
        // Arrange
        var auditLogEntry = Entry() with
        {
            Snapshot = new Snapshot("""{"a": 1, "b": 2}"""),
            CreatedAt = new CreatedAt(new DateTimeOffset(2026, 8, 28, 10, 0, 0, TimeSpan.Zero).AddTicks(1234560))
        };

        // Act
        var hash = AuditChainHasher.Compute(auditLogEntry);

        // Assert
        hash.ToString().Should().Be("8fcbf24fa924c0f99b774af3f0635a83429c7c8f61fe336d513264606f919641");
    }

    [Fact]
    public void Compute_MatchesPostgresForAnEntryWithAnActorAndAPredecessor()
    {
        // Arrange
        var auditLogEntry = Entry() with
        {
            AuditLogId = new AuditLogId(new Guid("44444444-4444-4444-4444-444444444444")),
            Operation = AuditOperation.Updated,
            Snapshot = new Snapshot("""{"a": 1, "b": 3}"""),
            ChangedByUserId = new ApplicationUserId(ChangedByUserId),
            Sequence = new Sequence(2),
            PreviousHash = new Hash("8fcbf24fa924c0f99b774af3f0635a83429c7c8f61fe336d513264606f919641"),
            CreatedAt = new CreatedAt(new DateTimeOffset(2026, 8, 28, 10, 0, 5, TimeSpan.Zero).AddTicks(10))
        };

        // Act
        var hash = AuditChainHasher.Compute(auditLogEntry);

        // Assert
        hash.ToString().Should().Be("4a59c1a49936c3a939787877db9cfa75286dbf1f6c6206640d0f08356e1b5bb9");
    }

    [Fact]
    public void Compute_MatchesPostgresForAnEntryWithNoOrganization()
    {
        // Arrange
        var auditLogEntry = Entry() with
        {
            AuditLogId = new AuditLogId(new Guid("66666666-6666-6666-6666-666666666666")),
            EntityId = new Guid("77777777-7777-7777-7777-777777777777"),
            OrganizationId = null,
            Snapshot = new Snapshot("""{"x": true}"""),
            CreatedAt = new CreatedAt(new DateTimeOffset(2026, 8, 28, 10, 0, 6, TimeSpan.Zero))
        };

        // Act
        var hash = AuditChainHasher.Compute(auditLogEntry);

        // Assert
        hash.ToString().Should().Be("cd5ae3ca3de5d9d47a7733657fc6f1ecb19c4bfde0afcc8c41bab42f59d409b4");
    }

    [Fact]
    public void Compute_ChangesWhenAnyAuditedFieldChanges()
    {
        // Arrange
        var auditLogEntry = Entry();
        var original = AuditChainHasher.Compute(auditLogEntry);

        // Act
        var edited = AuditChainHasher.Compute(auditLogEntry with { Snapshot = new Snapshot("""{"a": 2}""") });
        var moved = AuditChainHasher.Compute(auditLogEntry with { Sequence = new Sequence(9) });
        var reattributed = AuditChainHasher.Compute(
            auditLogEntry with { ChangedByUserId = new ApplicationUserId(ChangedByUserId) });

        // Assert
        edited.Should().NotBe(original);
        moved.Should().NotBe(original);
        reattributed.Should().NotBe(original);
    }

    [Fact]
    public void Compute_IgnoresPrecisionFinerThanTheColumnKeeps()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 8, 28, 10, 0, 0, TimeSpan.Zero).AddTicks(1234560);
        var auditLogEntry = Entry() with { CreatedAt = new CreatedAt(createdAt) };

        // Act
        var withSubMicrosecondTicks = AuditChainHasher.Compute(
            auditLogEntry with { CreatedAt = new CreatedAt(createdAt.AddTicks(7)) });

        // Assert
        withSubMicrosecondTicks.Should().Be(AuditChainHasher.Compute(auditLogEntry));
    }

    private static AuditLogEntry Entry()
    {
        return new AuditLogEntry
        {
            AuditLogId = new AuditLogId(AuditLogId),
            EntityType = AuditableEntityType.Case,
            EntityId = EntityId,
            OrganizationId = new OrganizationId(OrganizationId),
            Operation = AuditOperation.Created,
            Snapshot = new Snapshot("""{"a": 1, "b": 2}"""),
            ChangedByUserId = null,
            ChangedByApiClientId = null,
            Sequence = new Sequence(1),
            PreviousHash = null,
            CreatedAt = new CreatedAt(new DateTimeOffset(2026, 8, 28, 10, 0, 0, TimeSpan.Zero).AddTicks(1234560)),
            Hash = new Hash(new string('0', Hash.Length))
        };
    }
}
