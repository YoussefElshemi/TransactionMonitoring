using Core.Enums;
using Core.Models.Simulations;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Infrastructure.Repositories;

public class SimulationRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_CalledTwiceInOneContext_UpdatesTrackedSimulation()
    {
        var simulationId = Guid.NewGuid();
        var now = TestTimeConstants.Today;
        var modeContext = new ModeContext { Current = Mode.Test };
        var options = new DbContextOptionsBuilder<TransactionMonitoringDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            .Options;

        await using var context = new TransactionMonitoringDbContext(options, modeContext);
        context.Simulations.Add(new SimulationEntity
        {
            ModeId = (int)Mode.Test,
            SimulationId = simulationId,
            OrganizationId = Guid.NewGuid(),
            ScenarioId = (int)SimulationScenario.Baseline,
            StatusId = (int)SimulationStatus.Pending,
            TotalPayments = 10,
            ProcessedCount = 0,
            Seed = 1,
            CreatedByUserId = Guid.NewGuid(),
            CompletedAt = null,
            FailureReason = null,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new SimulationRepository(context, new MockTimeProvider(now));

        await repository.UpdateAsync(Request(simulationId, SimulationStatus.Running, 5), CancellationToken.None);
        await context.SaveChangesAsync();
        await repository.UpdateAsync(Request(simulationId, SimulationStatus.Completed, 10), CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var simulation = await context.Simulations.SingleAsync();
        simulation.StatusId.Should().Be((int)SimulationStatus.Completed);
        simulation.ProcessedCount.Should().Be(10);
    }

    private static UpdateSimulationRequest Request(Guid simulationId, SimulationStatus status, int processedCount)
    {
        return new UpdateSimulationRequest
        {
            SimulationId = new SimulationId(simulationId),
            Status = status,
            ProcessedCount = new ProcessedCount(processedCount),
            CompletedAt = null,
            FailureReason = null
        };
    }
}
