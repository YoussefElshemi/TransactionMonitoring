using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Approvals;
using Presentation.Mappers.Common;
using Presentation.Models.Approvals;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.ApprovalPoliciesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/approval-policy")]
public class OrganizationApprovalPolicyController(
    IApprovalService approvalService,
    IValidator<OrganizationApprovalPolicyRequestDto> organizationApprovalPolicyRequestValidator,
    IValidator<SetOrganizationApprovalPolicyRequestDto> setOrganizationApprovalPolicyRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<ApprovalPolicyResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApprovalPolicy(OrganizationApprovalPolicyRequestDto organizationApprovalPolicyRequestDto)
    {
        await organizationApprovalPolicyRequestValidator.ValidateAndThrowAsync(organizationApprovalPolicyRequestDto, HttpContext.RequestAborted);

        var approvalPolicy = await approvalService.FindPolicyAsync(
            new OrganizationId(organizationApprovalPolicyRequestDto.OrganizationId), HttpContext.RequestAborted);

        if (approvalPolicy is null)
        {
            return NotFound();
        }

        return Ok(approvalPolicy.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ApprovalPoliciesWrite)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetApprovalPolicy(SetOrganizationApprovalPolicyRequestDto setOrganizationApprovalPolicyRequestDto)
    {
        await setOrganizationApprovalPolicyRequestValidator.ValidateAndThrowAsync(setOrganizationApprovalPolicyRequestDto, HttpContext.RequestAborted);

        await approvalService.SetPolicyAsync(setOrganizationApprovalPolicyRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }
}
