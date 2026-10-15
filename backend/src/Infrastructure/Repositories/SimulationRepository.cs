using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Simulations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SimulationRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListSimulationRequest, SimulationEntity, Simulation>, ISimulationRepository
{
    public async Task AddAsync(Simulation simulation, CancellationToken stoppingToken)
    {
        await context.Simulations.AddAsync(simulation.ToEntity(), stoppingToken);
    }

    public async Task<Simulation> GetAsync(SimulationId simulationId, CancellationToken stoppingToken)
    {
        var simulationEntity = await context.Simulations
            .FirstAsync(x => x.SimulationId == simulationId, stoppingToken);

        return simulationEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, SimulationId simulationId, CancellationToken stoppingToken)
    {
        var exists = await context.Simulations
            .AnyAsync(x => x.SimulationId == simulationId && x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> HasRunningAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var exists = await context.Simulations
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           (x.StatusId == (int)SimulationStatus.Pending || x.StatusId == (int)SimulationStatus.Running),
                stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdateSimulationRequest updateSimulationRequest, CancellationToken stoppingToken)
    {
        var simulationEntity = await context.Simulations
            .FindAsync([(Guid)updateSimulationRequest.SimulationId], stoppingToken);

        simulationEntity!.StatusId = (int)updateSimulationRequest.Status;
        simulationEntity.ProcessedCount = updateSimulationRequest.ProcessedCount;
        simulationEntity.CompletedAt = updateSimulationRequest.CompletedAt;
        simulationEntity.FailureReason = updateSimulationRequest.FailureReason?.ToString();
        simulationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Simulations.Update(simulationEntity);
    }

    protected override Task<List<Simulation>> MapPageAsync(List<SimulationEntity> items, ListSimulationRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<SimulationEntity> OrderByExpression()
    {
        return new OrderByExpression<SimulationEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<SimulationEntity> FilterByPagedRequest(ListSimulationRequest request)
    {
        return context.Simulations.Where(x => x.OrganizationId == request.OrganizationId);
    }

    protected override IQueryable<SimulationEntity> FilterByDateRange(IQueryable<SimulationEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
