using System.Text;
using AutoFixture;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Infrastructure.Services;

public class SignatureVirusScannerTests : BaseTestClass
{
    private const string EicarSignature =
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private readonly Mock<IAttachmentStore> _mockAttachmentStore = new();
    private readonly SignatureVirusScanner _sut;

    public SignatureVirusScannerTests()
    {
        _sut = new SignatureVirusScanner(_mockAttachmentStore.Object);
    }

    [Fact]
    public async Task ScanAsync_GivenTheEicarSignature_ReportsInfected()
    {
        // Arrange
        var storageKey = StoredContent(EicarSignature);

        // Act
        var scanResult = await _sut.ScanAsync(storageKey, CancellationToken.None);

        // Assert
        scanResult.ScanStatus.Should().Be(AttachmentScanStatus.Infected);
        scanResult.ThreatName.Should().Be(new ThreatName("Eicar-Test-Signature"));
    }

    [Fact]
    public async Task ScanAsync_GivenTheEicarSignatureEmbeddedInOtherContent_ReportsInfected()
    {
        // Arrange
        var storageKey = StoredContent($"a bank statement, and then {EicarSignature} buried in it");

        // Act
        var scanResult = await _sut.ScanAsync(storageKey, CancellationToken.None);

        // Assert
        scanResult.ScanStatus.Should().Be(AttachmentScanStatus.Infected);
    }

    [Fact]
    public async Task ScanAsync_GivenOrdinaryContent_ReportsClean()
    {
        // Arrange
        var storageKey = StoredContent("an entirely unremarkable bank statement");

        // Act
        var scanResult = await _sut.ScanAsync(storageKey, CancellationToken.None);

        // Assert
        scanResult.ScanStatus.Should().Be(AttachmentScanStatus.Clean);
        scanResult.ThreatName.Should().BeNull();
    }

    [Fact]
    public async Task ScanAsync_GivenAnEmptyFile_ReportsClean()
    {
        // Arrange
        var storageKey = StoredContent(string.Empty);

        // Act
        var scanResult = await _sut.ScanAsync(storageKey, CancellationToken.None);

        // Assert
        scanResult.ScanStatus.Should().Be(AttachmentScanStatus.Clean);
    }

    private StorageKey StoredContent(string content)
    {
        var storageKey = new StorageKey(Fixture.Create<string>());

        _mockAttachmentStore
            .Setup(x => x.ReadAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(Encoding.ASCII.GetBytes(content)));

        return storageKey;
    }
}
