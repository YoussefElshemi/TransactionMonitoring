using Core.Enums;
using Core.Models.Simulations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class SimulationMapper
{
    public static Simulation ToModel(this SimulationEntity simulationEntity)
    {
        return new Simulation
        {
            Mode = (Mode)simulationEntity.ModeId,
            SimulationId = new SimulationId(simulationEntity.SimulationId),
            OrganizationId = new OrganizationId(simulationEntity.OrganizationId),
            Scenario = (SimulationScenario)simulationEntity.ScenarioId,
            Status = (SimulationStatus)simulationEntity.StatusId,
            TotalPayments = new PaymentCount(simulationEntity.TotalPayments),
            ProcessedCount = new ProcessedCount(simulationEntity.ProcessedCount),
            Seed = new SimulationSeed(simulationEntity.Seed),
            CreatedByUserId = new ApplicationUserId(simulationEntity.CreatedByUserId),
            CompletedAt = simulationEntity.CompletedAt.HasValue ? new CompletedAt(simulationEntity.CompletedAt.Value) : null,
            FailureReason = simulationEntity.FailureReason is null ? null : new FailureReason(simulationEntity.FailureReason),
            CreatedAt = new CreatedAt(simulationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(simulationEntity.UpdatedAt)
        };
    }

    public static SimulationEntity ToEntity(this Simulation simulation)
    {
        return new SimulationEntity
        {
            ModeId = (int)simulation.Mode,
            SimulationId = simulation.SimulationId,
            OrganizationId = simulation.OrganizationId,
            ScenarioId = (int)simulation.Scenario,
            StatusId = (int)simulation.Status,
            TotalPayments = simulation.TotalPayments,
            ProcessedCount = simulation.ProcessedCount,
            Seed = simulation.Seed,
            CreatedByUserId = simulation.CreatedByUserId,
            CompletedAt = simulation.CompletedAt,
            FailureReason = simulation.FailureReason?.ToString(),
            CreatedAt = simulation.CreatedAt,
            UpdatedAt = simulation.UpdatedAt
        };
    }
}
