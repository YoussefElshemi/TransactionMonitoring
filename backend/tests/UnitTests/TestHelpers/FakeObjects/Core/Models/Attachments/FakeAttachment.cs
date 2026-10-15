using AutoFixture;
using Core.Enums;
using Core.Models.Attachments;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Attachments;

public static class FakeAttachment
{
    public static Attachment CreateValid(IFixture fixture)
    {
        return new Attachment
        {
            Mode = Mode.Live,
            AttachmentId = new AttachmentId(fixture.Create<Guid>()),
            CaseId = FakeCaseId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            FileName = new FileName("statement.pdf"),
            ContentType = new ContentType("application/pdf"),
            ByteCount = new ByteCount(fixture.Create<int>() + 1),
            StorageKey = new StorageKey(fixture.Create<string>()),
            Checksum = new Checksum(fixture.Create<string>()),
            ScanStatus = AttachmentScanStatus.Pending,
            ScannedAt = null,
            ThreatName = null,
            UploadedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
