using System.Text;
using Core.Documents;
using Core.Enums;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Documents;

public class AuditLogCsvDocumentTests : BaseTestClass
{
    [Fact]
    public void Write_LeadsWithAHeaderRowAndOneRowPerEntry()
    {
        // Arrange
        var auditLogEntries = new List<AuditLogEntry> { Entry(1, null), Entry(2, new Hash(new string('a', Hash.Length))) };

        // Act
        var lines = Read(AuditLogCsvDocument.Write(auditLogEntries));

        // Assert
        lines.Should().HaveCount(3);
        lines[0].Should().StartWith("Sequence,AuditLogId,CreatedAt");
        lines[1].Should().StartWith("\"1\"");
        lines[2].Should().StartWith("\"2\"");
    }

    [Fact]
    public void Write_CarriesTheSnapshotAndBothHashesSoTheFileCanBeCheckedWithoutUs()
    {
        // Arrange
        var previousHash = new Hash(new string('a', Hash.Length));
        var auditLogEntry = Entry(2, previousHash);

        // Act
        var lines = Read(AuditLogCsvDocument.Write([auditLogEntry]));

        // Assert
        lines[1].Should().Contain("""{""amount"": 1}""");
        lines[1].Should().Contain(previousHash.ToString());
        lines[1].Should().Contain(auditLogEntry.Hash.ToString());
    }

    [Fact]
    public void Write_DoublesQuotesRatherThanLettingThemEndAField()
    {
        // Arrange
        var auditLogEntry = Entry(1, null) with
        {
            Snapshot = new Snapshot("""{"note": "declined, then reopened"}""")
        };

        // Act
        var lines = Read(AuditLogCsvDocument.Write([auditLogEntry]));

        // Assert
        lines.Should().HaveCount(2);
        lines[1].Should().Contain("""{""note"": ""declined, then reopened""}""");
    }

    [Fact]
    public void Write_StartsWithAByteOrderMark()
    {
        // Act
        var content = AuditLogCsvDocument.Write([Entry(1, null)]);

        // Assert
        content.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
    }

    [Fact]
    public void Write_LeavesAbsentActorsAndAbsentPredecessorsEmptyRatherThanNull()
    {
        // Act
        var lines = Read(AuditLogCsvDocument.Write([Entry(1, null)]));

        // Assert
        lines[1].Should().Contain(",\"\",\"\",");
        lines[1].Should().NotContain("null");
    }

    private static List<string> Read(byte[] content)
    {
        return Encoding.UTF8.GetString(content)
            .TrimStart('﻿')
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static AuditLogEntry Entry(long sequence, Hash? previousHash)
    {
        return new AuditLogEntry
        {
            AuditLogId = new AuditLogId(Guid.NewGuid()),
            EntityType = AuditableEntityType.Case,
            EntityId = Guid.NewGuid(),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            Operation = AuditOperation.Created,
            Snapshot = new Snapshot("""{"amount": 1}"""),
            ChangedByUserId = null,
            ChangedByApiClientId = null,
            Sequence = new Sequence(sequence),
            PreviousHash = previousHash,
            Hash = new Hash(new string('b', Hash.Length)),
            CreatedAt = new CreatedAt(new DateTimeOffset(2026, 8, 28, 10, 0, 0, TimeSpan.Zero))
        };
    }
}
