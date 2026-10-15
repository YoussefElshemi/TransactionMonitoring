using Core.Models.Simulations;
using Core.ValueObjects;
using Presentation.Models.Simulations;

namespace Presentation.Mappers.Simulations;

public static class SimulationDtoMapper
{
    public static SimulationResponseDto ToDto(this Simulation simulation)
    {
        return new SimulationResponseDto
        {
            Mode = simulation.Mode.ToString(),
            SimulationId = simulation.SimulationId,
            OrganizationId = simulation.OrganizationId,
            Scenario = simulation.Scenario.ToString(),
            Status = simulation.Status.ToString(),
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

    public static ListSimulationRequest ToModel(this ListSimulationRequestDto listSimulationRequestDto)
    {
        return new ListSimulationRequest
        {
            OrganizationId = new OrganizationId(listSimulationRequestDto.OrganizationId),
            PageNumber = new PageNumber(listSimulationRequestDto.PageNumber),
            PageSize = new PageSize(listSimulationRequestDto.PageSize),
            FromDate = new DateRange(listSimulationRequestDto.FromDate),
            ToDate = new DateRange(listSimulationRequestDto.ToDate)
        };
    }

    public static ListSimulationRequest ToModel(this ListMySimulationRequestDto listMySimulationRequestDto)
    {
        return new ListSimulationRequest
        {
            OrganizationId = new OrganizationId(listMySimulationRequestDto.OrganizationId),
            PageNumber = new PageNumber(listMySimulationRequestDto.PageNumber),
            PageSize = new PageSize(listMySimulationRequestDto.PageSize),
            FromDate = new DateRange(listMySimulationRequestDto.FromDate),
            ToDate = new DateRange(listMySimulationRequestDto.ToDate)
        };
    }
}
