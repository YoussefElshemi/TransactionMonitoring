using AutoFixture;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.IdempotencyRecords;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.IdempotencyRecords;

namespace UnitTests.Tests.Core.Services;

public class IdempotencyServiceTests : BaseTestClass
{
    private readonly Mock<IIdempotencyRecordRepository> _mockIdempotencyRecordRepository = new();
    private readonly IdempotencyService _sut;

    public IdempotencyServiceTests()
    {
        _sut = new IdempotencyService(
            _mockIdempotencyRecordRepository.Object,
            Options.Create(FakeAppConfig.CreateValid(Fixture)),
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task ReserveAsync_KeyIsUnused_ReturnsReserved()
    {
        // Arrange
        _mockIdempotencyRecordRepository
            .Setup(x => x.TryReserveAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);

        // Act
        var reservation = await _sut.ReserveAsync(CreateRequest(), CancellationToken.None);

        // Assert
        reservation.Outcome.Should().Be(IdempotencyOutcome.Reserved);
        reservation.Record.Should().BeNull();
    }

    [Fact]
    public async Task ReserveAsync_KeyHasCompletedResponse_ReturnsReplay()
    {
        // Arrange
        var existing = FakeIdempotencyRecord.CreateValid(Fixture) with
        {
            Resource = new Resource("POST /rules/evaluate"),
            StatusCode = new StatusCode(200)
        };

        _mockIdempotencyRecordRepository
            .Setup(x => x.TryReserveAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var reservation = await _sut.ReserveAsync(CreateRequest(), CancellationToken.None);

        // Assert
        reservation.Outcome.Should().Be(IdempotencyOutcome.Replay);
        reservation.Record.Should().Be(existing);
    }

    [Fact]
    public async Task ReserveAsync_KeyIsStillRunning_ReturnsInProgress()
    {
        // Arrange
        var existing = FakeIdempotencyRecord.CreateValid(Fixture) with
        {
            Resource = new Resource("POST /rules/evaluate"),
            StatusCode = new StatusCode(0)
        };

        _mockIdempotencyRecordRepository
            .Setup(x => x.TryReserveAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var reservation = await _sut.ReserveAsync(CreateRequest(), CancellationToken.None);

        // Assert
        reservation.Outcome.Should().Be(IdempotencyOutcome.InProgress);
        reservation.Record.Should().BeNull();
    }

    [Fact]
    public async Task ReserveAsync_KeyWasUsedForAnotherEndpoint_ReturnsResourceMismatch()
    {
        // Arrange
        var existing = FakeIdempotencyRecord.CreateValid(Fixture) with
        {
            Resource = new Resource("POST /rule-sets/evaluate"),
            StatusCode = new StatusCode(200)
        };

        _mockIdempotencyRecordRepository
            .Setup(x => x.TryReserveAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var reservation = await _sut.ReserveAsync(CreateRequest(), CancellationToken.None);

        // Assert
        reservation.Outcome.Should().Be(IdempotencyOutcome.ResourceMismatch);
        reservation.Record.Should().BeNull();
    }

    [Fact]
    public async Task ReserveAsync_GivenRequest_ReservesWithConfiguredRetention()
    {
        // Arrange
        IdempotencyRecord? reserved = null;

        _mockIdempotencyRecordRepository
            .Setup(x => x.TryReserveAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()))
            .Callback<IdempotencyRecord, CancellationToken>((x, _) => reserved = x)
            .ReturnsAsync((IdempotencyRecord?)null);

        // Act
        await _sut.ReserveAsync(CreateRequest(), CancellationToken.None);

        // Assert
        reserved.Should().NotBeNull();
        ((DateTimeOffset)reserved!.ExpiresAt).Should().Be(MockTimeProvider.GetUtcNow().AddHours(24));
        ((int)reserved.StatusCode).Should().Be(0);
        ((string)reserved.ResponseBody).Should().BeEmpty();
    }

    private CreateIdempotencyRecordRequest CreateRequest()
    {
        return new CreateIdempotencyRecordRequest
        {
            IdempotencyKey = new IdempotencyKey(Fixture.Create<Guid>()),
            OrganizationId = new OrganizationId(Fixture.Create<Guid>()),
            Resource = new Resource("POST /rules/evaluate")
        };
    }
}
