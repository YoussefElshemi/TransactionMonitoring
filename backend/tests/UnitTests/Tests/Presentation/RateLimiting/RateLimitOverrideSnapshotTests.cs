using Core.Configs;
using Core.Enums;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.RateLimiting;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.RateLimiting;

public class RateLimitOverrideSnapshotTests : BaseTestClass
{
    private readonly RateLimitOverrideSnapshot _sut = new();

    [Fact]
    public void Find_NoOverrides_ReturnsNull()
    {
        // Arrange
        _sut.Replace([]);

        // Act
        var found = _sut.Find(Guid.NewGuid(), RateLimitPool.Read);

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void Find_PoolSpecificOverride_AppliesToThatPoolOnly()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _sut.Replace([Override(organizationId, RateLimitPool.Evaluation, permitLimit: 10)]);

        // Act
        var evaluation = _sut.Find(organizationId, RateLimitPool.Evaluation);
        var read = _sut.Find(organizationId, RateLimitPool.Read);

        // Assert
        evaluation.Should().NotBeNull();
        read.Should().BeNull();
    }

    [Fact]
    public void Find_AllPoolsOverride_AppliesToEveryPool()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _sut.Replace([Override(organizationId, pool: null, permitLimit: 10)]);

        // Act
        foreach (var pool in Enum.GetValues<RateLimitPool>())
        {
            _sut.Find(organizationId, pool).Should().NotBeNull();
        }
    }

    [Fact]
    public void Find_PoolSpecificBesideAllPools_TheNarrowerOneWins()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _sut.Replace([
            Override(organizationId, pool: null, permitLimit: 10),
            Override(organizationId, RateLimitPool.Evaluation, permitLimit: 900)
        ]);

        // Act
        var evaluation = _sut.Find(organizationId, RateLimitPool.Evaluation);
        var read = _sut.Find(organizationId, RateLimitPool.Read);

        // Assert
        ((int)evaluation!.PermitLimit).Should().Be(900);
        ((int)read!.PermitLimit).Should().Be(10);
    }

    [Fact]
    public void Find_OverrideForAnotherOrganisation_DoesNotLeak()
    {
        // Arrange
        _sut.Replace([Override(Guid.NewGuid(), RateLimitPool.Read, permitLimit: 10)]);

        // Act
        var found = _sut.Find(Guid.NewGuid(), RateLimitPool.Read);

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void Replace_CalledAgain_DropsWhatIsNoLongerInForce()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _sut.Replace([Override(organizationId, RateLimitPool.Read, permitLimit: 10)]);

        // Act
        _sut.Replace([]);

        // Assert
        _sut.Find(organizationId, RateLimitPool.Read).Should().BeNull();
    }

    [Fact]
    public void ToBucketConfig_Override_ReplacesLimitsButKeepsTheRest()
    {
        // Arrange
        var configured = new RateLimitBucketConfig
        {
            PermitLimit = 3_000,
            TokensPerPeriod = 3_000,
            ReplenishmentPeriodSeconds = 60,
            QueueLimit = 7
        };

        var rateLimitOverride = Override(Guid.NewGuid(), RateLimitPool.Read, permitLimit: 10);

        // Act
        var bucket = RateLimitOverrideSnapshot.ToBucketConfig(rateLimitOverride, configured);

        // Assert
        bucket.PermitLimit.Should().Be(10);
        bucket.TokensPerPeriod.Should().Be(10);
        bucket.QueueLimit.Should().Be(7);
    }

    private static RateLimitOverride Override(Guid organizationId, RateLimitPool? pool, int permitLimit)
    {
        var now = DateTimeOffset.UtcNow;

        return new RateLimitOverride
        {
            RateLimitOverrideId = new RateLimitOverrideId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(organizationId),
            Pool = pool,
            PermitLimit = new PermitLimit(permitLimit),
            TokensPerPeriod = new TokensPerPeriod(permitLimit),
            ReplenishmentPeriod = new ReplenishmentPeriod(60),
            Reason = new RateLimitOverrideReason("Incident 4021 — retry loop from a single client"),
            ExpiresAt = new ExpiresAt(now.AddHours(1)),
            CreatedByUserId = new ApplicationUserId(Guid.NewGuid()),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
