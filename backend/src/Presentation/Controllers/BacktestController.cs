using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.BackTests;
using Presentation.Mappers.Common;
using Presentation.Models.BackTests;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.BacktestsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("backTests")]
public class BackTestController(
    IApplicationUserService applicationUserService,
    IBackTestService backTestService,
    IBackTestResultService backTestResultService,
    BackTestResponseFactory backTestResponseFactory,
    IValidator<BackTestRequestDto> backTestRequestValidator,
    IValidator<CreateBackTestRequestDto> createBackTestRequestValidator,
    IValidator<ListBackTestRequestDto> listBackTestRequestValidator,
    IValidator<ListBackTestResultRequestDto> listBackTestResultRequestValidator) : Controller
{
    [RequirePermission(Permission.BacktestsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("/rules/{RuleId}/backTests")]
    [ProducesResponseType<ResponseDataDto<BackTestResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBackTest(CreateBackTestRequestDto createBackTestRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createBackTestRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        createBackTestRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createBackTestRequestValidator.ValidateAndThrowAsync(createBackTestRequestDto, HttpContext.RequestAborted);

        var backTest = await backTestService.CreateAsync(createBackTestRequestDto.ToModel(), HttpContext.RequestAborted);

        var resource = await backTestResponseFactory.CreateAsync(
            backTest, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<BackTestResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBackTests([FromQuery] ListBackTestRequestDto listBackTestRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listBackTestRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listBackTestRequestValidator.ValidateAndThrowAsync(listBackTestRequestDto, HttpContext.RequestAborted);

        var backTests = await backTestService.ListAsync(listBackTestRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await backTestResponseFactory.CreateAsync(
            backTests.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(backTests.ToDto(resources));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{BackTestId:guid}")]
    [ProducesResponseType<ResponseDataDto<BackTestResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBackTest(BackTestRequestDto backTestRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        backTestRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await backTestRequestValidator.ValidateAndThrowAsync(backTestRequestDto, HttpContext.RequestAborted);

        var backTest = await backTestService.GetAsync(new BackTestId(backTestRequestDto.BackTestId), HttpContext.RequestAborted);

        var resource = await backTestResponseFactory.CreateAsync(
            backTest, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{BackTestId:guid}/results")]
    [ProducesResponseType<PagedResponseDto<BackTestResultResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBackTestResults([FromQuery] ListBackTestResultRequestDto listBackTestResultRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listBackTestResultRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listBackTestResultRequestValidator.ValidateAndThrowAsync(listBackTestResultRequestDto, HttpContext.RequestAborted);

        var results = await backTestResultService.ListAsync(listBackTestResultRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(results.ToDto(BackTestResultDtoMapper.ToDto));
    }
}
