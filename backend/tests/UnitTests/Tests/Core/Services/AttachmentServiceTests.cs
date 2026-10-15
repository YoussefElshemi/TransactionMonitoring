using System.Text;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Attachments;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Attachments;

namespace UnitTests.Tests.Core.Services;

public class AttachmentServiceTests : BaseTestClass
{
    private readonly Mock<IAttachmentRepository> _mockAttachmentRepository = new();
    private readonly Mock<IAttachmentStore> _mockAttachmentStore = new();
    private readonly Mock<IVirusScanner> _mockVirusScanner = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly AttachmentService _sut;

    public AttachmentServiceTests()
    {
        _sut = new AttachmentService(
            _mockAttachmentRepository.Object,
            _mockAttachmentStore.Object,
            _mockVirusScanner.Object,
            _mockUnitOfWork.Object,
            MockLogger.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task CreateAsync_WhenTheScannerReportsInfected_PurgesTheBytesAndKeepsTheRow()
    {
        // Arrange
        var attachment = ArrangeCreate();
        ArrangeScan(AttachmentScanStatus.Infected, new ThreatName("Nasty"));

        // Act
        await _sut.CreateAsync(CreateRequest(attachment), CancellationToken.None);

        // Assert
        _mockAttachmentStore.Verify(x => x.DeleteAsync(attachment.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockAttachmentRepository.Verify(
            x => x.RecordScanResultAsync(
                It.IsAny<AttachmentId>(),
                It.Is<ScanResult>(y => y.ScanStatus == AttachmentScanStatus.Infected),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenTheScannerReportsClean_LeavesTheBytesInPlace()
    {
        // Arrange
        var attachment = ArrangeCreate();
        ArrangeScan(AttachmentScanStatus.Clean, null);

        // Act
        await _sut.CreateAsync(CreateRequest(attachment), CancellationToken.None);

        // Assert
        _mockAttachmentStore.Verify(x => x.DeleteAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTheScannerThrows_RecordsFailedRatherThanFailingTheUpload()
    {
        // Arrange
        var attachment = ArrangeCreate();
        _mockVirusScanner
            .Setup(x => x.ScanAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("scanner unreachable"));

        // Act
        await _sut.CreateAsync(CreateRequest(attachment), CancellationToken.None);

        // Assert
        _mockAttachmentRepository.Verify(
            x => x.RecordScanResultAsync(
                It.IsAny<AttachmentId>(),
                It.Is<ScanResult>(y => y.ScanStatus == AttachmentScanStatus.Failed && y.ThreatName == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAttachmentStore.Verify(x => x.DeleteAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AttachmentScanStatus.Pending)]
    [InlineData(AttachmentScanStatus.Infected)]
    [InlineData(AttachmentScanStatus.Failed)]
    public async Task DownloadAsync_WhenTheFileIsNotClean_IsRefused(AttachmentScanStatus scanStatus)
    {
        // Arrange
        var attachment = FakeAttachment.CreateValid(Fixture) with { ScanStatus = scanStatus };
        _mockAttachmentRepository
            .Setup(x => x.GetAsync(attachment.AttachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);

        // Act
        var act = async () => await _sut.DownloadAsync(attachment.AttachmentId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockAttachmentStore.Verify(x => x.ReadAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenTheFileIsClean_ReadsTheBytes()
    {
        // Arrange
        var attachment = FakeAttachment.CreateValid(Fixture) with { ScanStatus = AttachmentScanStatus.Clean };
        _mockAttachmentRepository
            .Setup(x => x.GetAsync(attachment.AttachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        _mockAttachmentStore
            .Setup(x => x.ReadAsync(attachment.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("statement")));

        // Act
        var attachmentContent = await _sut.DownloadAsync(attachment.AttachmentId, CancellationToken.None);

        // Assert
        attachmentContent.Attachment.AttachmentId.Should().Be(attachment.AttachmentId);
        attachmentContent.Content.Should().NotBeNull();
    }

    [Theory]
    [InlineData(AttachmentScanStatus.Clean)]
    [InlineData(AttachmentScanStatus.Infected)]
    public async Task RescanAsync_WhenTheFileAlreadyHasAVerdict_IsRefused(AttachmentScanStatus scanStatus)
    {
        // Arrange
        var attachment = FakeAttachment.CreateValid(Fixture) with { ScanStatus = scanStatus };
        _mockAttachmentRepository
            .Setup(x => x.GetAsync(attachment.AttachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);

        // Act
        var act = async () => await _sut.RescanAsync(attachment.AttachmentId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockVirusScanner.Verify(x => x.ScanAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RescanAsync_WhenTheScanPreviouslyFailed_AsksTheScannerAgain()
    {
        // Arrange
        var attachment = FakeAttachment.CreateValid(Fixture) with { ScanStatus = AttachmentScanStatus.Failed };
        _mockAttachmentRepository
            .Setup(x => x.GetAsync(attachment.AttachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        ArrangeScan(AttachmentScanStatus.Clean, null);

        // Act
        await _sut.RescanAsync(attachment.AttachmentId, CancellationToken.None);

        // Assert
        _mockVirusScanner.Verify(x => x.ScanAsync(attachment.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Attachment ArrangeCreate()
    {
        var attachment = FakeAttachment.CreateValid(Fixture);

        _mockAttachmentStore
            .Setup(x => x.WriteAsync(It.IsAny<OrganizationId>(), It.IsAny<AttachmentId>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredAttachment
            {
                StorageKey = attachment.StorageKey,
                ByteCount = attachment.ByteCount,
                Checksum = attachment.Checksum
            });

        _mockAttachmentRepository
            .Setup(x => x.GetAsync(It.IsAny<AttachmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);

        return attachment;
    }

    private void ArrangeScan(AttachmentScanStatus scanStatus, ThreatName? threatName)
    {
        _mockVirusScanner
            .Setup(x => x.ScanAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScanResult
            {
                ScanStatus = scanStatus,
                ThreatName = threatName
            });
    }

    private static CreateAttachmentRequest CreateRequest(Attachment attachment)
    {
        return new CreateAttachmentRequest
        {
            CaseId = attachment.CaseId,
            OrganizationId = attachment.OrganizationId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            Content = new MemoryStream(Encoding.UTF8.GetBytes("statement")),
            UploadedByUserId = attachment.UploadedByUserId
        };
    }
}
