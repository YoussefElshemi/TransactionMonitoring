using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Simulations;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Common;
using Presentation.Mappers.Sandbox;
using Presentation.Mappers.Simulations;
using Presentation.Mappers.Webhooks;
using Presentation.Models.Common;
using Presentation.Models.Sandbox;
using Presentation.Models.Simulations;
using Presentation.Models.Webhooks;

namespace Presentation.Controllers;

[RequirePermission(Permission.SandboxRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/sandbox")]
public class OrganizationSandboxController(
    IApplicationUserService applicationUserService,
    ISimulationService simulationService,
    ISandboxService sandboxService,
    ISandboxCreditService sandboxCreditService,
    SimulationResponseFactory simulationResponseFactory,
    WebhookDeliveryResponseFactory webhookDeliveryResponseFactory,
    IValidator<StartSimulationRequestDto> startSimulationRequestValidator,
    IValidator<GetSimulationRequestDto> getSimulationRequestValidator,
    IValidator<ListSimulationRequestDto> listSimulationRequestValidator,
    IValidator<ListMySimulationRequestDto> listMySimulationRequestValidator,
    IValidator<ResetSandboxRequestDto> resetSandboxRequestValidator,
    IValidator<GetSandboxCreditRequestDto> getSandboxCreditRequestValidator,
    IValidator<ReplayWebhookDeliveryRequestDto> replayWebhookDeliveryRequestValidator,
    IValidator<ListWebhookDeliveryRequestDto> listWebhookDeliveryRequestValidator,
    IValidator<ListMyWebhookDeliveryRequestDto> listMyWebhookDeliveryRequestValidator) : Controller
{
    [RequirePermission(Permission.SandboxWrite)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("simulations")]
    [ProducesResponseType<ResponseDataDto<SimulationResourceDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartSimulation(StartSimulationRequestDto startSimulationRequestDto, Guid organizationId)
    {
        startSimulationRequestDto = startSimulationRequestDto with { OrganizationId = organizationId };

        await startSimulationRequestValidator.ValidateAndThrowAsync(startSimulationRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var simulation = await simulationService.StartAsync(new StartSimulationRequest
        {
            OrganizationId = new OrganizationId(organizationId),
            Scenario = Enum.Parse<SimulationScenario>(startSimulationRequestDto.Body.Scenario, true),
            PaymentCount = new PaymentCount(startSimulationRequestDto.Body.PaymentCount),
            Seed = startSimulationRequestDto.Body.Seed.HasValue ? new SimulationSeed(startSimulationRequestDto.Body.Seed.Value) : null,
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await simulationResponseFactory.CreateAsync(
            simulation, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Accepted(resource.ToResponseData());
    }

    [HttpGet("simulations/{simulationId:guid}")]
    [ProducesResponseType<ResponseDataDto<SimulationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSimulation(Guid organizationId, Guid simulationId)
    {
        await getSimulationRequestValidator.ValidateAndThrowAsync(
            new GetSimulationRequestDto { OrganizationId = organizationId, SimulationId = simulationId }, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var simulation = await simulationService.GetAsync(new SimulationId(simulationId), HttpContext.RequestAborted);

        var resource = await simulationResponseFactory.CreateAsync(simulation, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [HttpGet("simulations")]
    [ProducesResponseType<PagedResponseDto<SimulationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSimulations([FromQuery] ListSimulationRequestDto listSimulationRequestDto)
    {
        await listSimulationRequestValidator.ValidateAndThrowAsync(listSimulationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var simulations = await simulationService.ListAsync(listSimulationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await simulationResponseFactory.CreateAsync(simulations.Data, context, HttpContext.RequestAborted);

        return Ok(simulations.ToDto(resources));
    }

    [HttpGet("credits")]
    [ProducesResponseType<ResponseDataDto<SandboxCreditResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSandboxCredits(Guid organizationId)
    {
        await getSandboxCreditRequestValidator.ValidateAndThrowAsync(
            new GetSandboxCreditRequestDto { OrganizationId = organizationId }, HttpContext.RequestAborted);

        var credit = await sandboxCreditService.GetAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        return Ok(credit.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SandboxReset)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("reset")]
    [ProducesResponseType<ResponseDataDto<SandboxResetResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetSandbox(ResetSandboxRequestDto resetSandboxRequestDto, Guid organizationId)
    {
        resetSandboxRequestDto = resetSandboxRequestDto with { OrganizationId = organizationId };

        await resetSandboxRequestValidator.ValidateAndThrowAsync(resetSandboxRequestDto, HttpContext.RequestAborted);

        var sandboxReset = await sandboxService.ResetAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        return Ok(sandboxReset.ToDto().ToResponseData());
    }

    [HttpGet("webhook-deliveries")]
    [ProducesResponseType<PagedResponseDto<WebhookDeliveryResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWebhookDeliveries([FromQuery] ListWebhookDeliveryRequestDto listWebhookDeliveryRequestDto)
    {
        await listWebhookDeliveryRequestValidator.ValidateAndThrowAsync(listWebhookDeliveryRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var deliveries = await sandboxService.ListAsync(listWebhookDeliveryRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await webhookDeliveryResponseFactory.CreateAsync(deliveries.Data, context, HttpContext.RequestAborted);

        return Ok(deliveries.ToDto(resources));
    }

    [RequirePermission(Permission.SandboxWrite)]
    [Idempotent]
    [HttpPost("webhook-deliveries/{outboxMessageId:guid}/replay")]
    [ProducesResponseType<ResponseDataDto<WebhookDeliveryResourceDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReplayWebhookDelivery(Guid organizationId, Guid outboxMessageId)
    {
        await replayWebhookDeliveryRequestValidator.ValidateAndThrowAsync(
            new ReplayWebhookDeliveryRequestDto { OrganizationId = organizationId, OutboxMessageId = outboxMessageId },
            HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var replay = await sandboxService.ReplayDeliveryAsync(new OutboxMessageId(outboxMessageId), HttpContext.RequestAborted);

        var resource = await webhookDeliveryResponseFactory.CreateAsync(replay, context, HttpContext.RequestAborted);

        return Accepted(resource.ToResponseData());
    }

    [HttpGet("/organizations/my/sandbox/credits")]
    [ProducesResponseType<ResponseDataDto<SandboxCreditResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySandboxCredits()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var credit = await sandboxCreditService.GetAsync(authenticatedUser.OrganizationId, HttpContext.RequestAborted);

        return Ok(credit.ToDto().ToResponseData());
    }

    [HttpGet("/organizations/my/sandbox/simulations")]
    [ProducesResponseType<PagedResponseDto<SimulationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMySimulations([FromQuery] ListMySimulationRequestDto listMySimulationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMySimulationRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await listMySimulationRequestValidator.ValidateAndThrowAsync(listMySimulationRequestDto, HttpContext.RequestAborted);

        var simulations = await simulationService.ListAsync(listMySimulationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await simulationResponseFactory.CreateAsync(
            simulations.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(simulations.ToDto(resources));
    }

    [HttpGet("/organizations/my/sandbox/webhook-deliveries")]
    [ProducesResponseType<PagedResponseDto<WebhookDeliveryResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyWebhookDeliveries([FromQuery] ListMyWebhookDeliveryRequestDto listMyWebhookDeliveryRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyWebhookDeliveryRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await listMyWebhookDeliveryRequestValidator.ValidateAndThrowAsync(listMyWebhookDeliveryRequestDto, HttpContext.RequestAborted);

        var deliveries = await sandboxService.ListAsync(listMyWebhookDeliveryRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await webhookDeliveryResponseFactory.CreateAsync(
            deliveries.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(deliveries.ToDto(resources));
    }
}
