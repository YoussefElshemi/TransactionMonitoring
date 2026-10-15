using Core.Enums;
using Core.Models.Availability;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Infrastructure.Repositories;

public class AvailabilityWindowRepositoryTests
{
    [Fact]
    public async Task MergeAsync_GivenANewWindow_TracksTheWindowAndBucketsAsAdded()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TransactionMonitoringDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            .Options;

        await using var context = new TransactionMonitoringDbContext(
            options,
            new ModeContext { Current = Mode.Live });

        var repository = new AvailabilityWindowRepository(context, new MockTimeProvider());
        var request = new MergeAvailabilityWindowRequest
        {
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            PeriodStart = new PeriodStart(TestTimeConstants.Today),
            TotalRequests = new RequestCount(2),
            FailedRequests = new RequestCount(1),
            TotalLatencyMilliseconds = new TotalLatencyMilliseconds(300),
            MaxLatencyMilliseconds = new LatencyMilliseconds(200),
            LatencyBuckets =
            [
                new MergeAvailabilityLatencyBucketRequest
                {
                    UpperBoundMilliseconds = new LatencyBucketBound(100),
                    Count = new RequestCount(1)
                },
                new MergeAvailabilityLatencyBucketRequest
                {
                    UpperBoundMilliseconds = new LatencyBucketBound(250),
                    Count = new RequestCount(1)
                }
            ]
        };

        // Act
        await repository.MergeAsync(request, CancellationToken.None);

        // Assert
        context.ChangeTracker.Entries<AvailabilityWindowEntity>()
            .Single()
            .State.Should().Be(EntityState.Added);

        context.ChangeTracker.Entries<AvailabilityWindowLatencyBucketEntity>()
            .Should().OnlyContain(x => x.State == EntityState.Added);
    }
}
