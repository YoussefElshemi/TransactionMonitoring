using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Sandbox;
using Presentation.Models.Simulations;

namespace Presentation.Validators.Sandbox;

public class StartSimulationRequestValidator : AbstractValidator<StartSimulationRequestDto>
{
    internal const int MaxPaymentCount = 5_000;

    public StartSimulationRequestValidator(IOrganizationService organizationService, ISimulationService simulationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => !await simulationService.HasRunningAsync(new OrganizationId(x.OrganizationId), ct))
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("A simulation is already running for this organization.");

        // IsEnumName, not TryParse: TryParse accepts numbers and "99" would fail later on the foreign key.
        RuleFor(x => x.Body.Scenario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .IsEnumName(typeof(SimulationScenario), false)
            .WithMessage($"Scenario must be one of: {string.Join(", ", Enum.GetNames<SimulationScenario>())}.");

        RuleFor(x => x.Body.PaymentCount)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxPaymentCount)
            .WithMessage($"A simulation must not ask for more than {MaxPaymentCount} payments.");

        RuleFor(x => x.Body.Seed!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Body.Seed.HasValue);
    }
}

public class GetSimulationRequestValidator : AbstractValidator<GetSimulationRequestDto>
{
    public GetSimulationRequestValidator(IOrganizationService organizationService, ISimulationService simulationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.SimulationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await simulationService.ExistsAsync(new OrganizationId(dto.OrganizationId), new SimulationId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Simulation not found");
    }
}

public class ListSimulationRequestValidator : AbstractValidator<ListSimulationRequestDto>
{
    public ListSimulationRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}

public class ListMySimulationRequestValidator : AbstractValidator<ListMySimulationRequestDto>
{
    public ListMySimulationRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}

public class ResetSandboxRequestValidator : AbstractValidator<ResetSandboxRequestDto>
{
    public ResetSandboxRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}

public class GetSandboxCreditRequestValidator : AbstractValidator<GetSandboxCreditRequestDto>
{
    public GetSandboxCreditRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}
