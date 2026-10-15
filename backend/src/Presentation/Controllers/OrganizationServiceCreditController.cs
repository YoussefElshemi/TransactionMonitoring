using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.ServiceCredits;
using Presentation.Models.Common;
using Presentation.Models.ServiceCredits;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/service-credits")]
public class OrganizationServiceCreditController(
    IServiceCreditService serviceCreditService,
    IValidator<IssueServiceCreditRequestDto> issueServiceCreditRequestValidator,
    IValidator<ListServiceCreditRequestDto> listServiceCreditRequestValidator) : Controller
{
    [RequirePermission(Permission.ServiceCreditsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ServiceCreditResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListServiceCredits([FromQuery] ListServiceCreditRequestDto listServiceCreditRequestDto)
    {
        await listServiceCreditRequestValidator.ValidateAndThrowAsync(listServiceCreditRequestDto, HttpContext.RequestAborted);

        var serviceCredits = await serviceCreditService.ListAsync(
            listServiceCreditRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(serviceCredits.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.ServiceCreditsIssue)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<ServiceCreditResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> IssueServiceCredit(IssueServiceCreditRequestDto issueServiceCreditRequestDto)
    {
        await issueServiceCreditRequestValidator.ValidateAndThrowAsync(issueServiceCreditRequestDto, HttpContext.RequestAborted);

        var serviceCredit = await serviceCreditService.IssueAsync(
            issueServiceCreditRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, serviceCredit.ToDto().ToResponseData());
    }
}
