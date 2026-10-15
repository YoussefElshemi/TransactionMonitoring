using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Discounts;
using Presentation.Models.Common;
using Presentation.Models.Discounts;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/contract-discounts")]
public class OrganizationContractDiscountController(
    IApplicationUserService applicationUserService,
    IContractDiscountService contractDiscountService,
    IValidator<CreateContractDiscountRequestDto> createContractDiscountRequestValidator,
    IValidator<ContractDiscountRequestDto> contractDiscountRequestValidator,
    IValidator<DeleteContractDiscountRequestDto> deleteContractDiscountRequestValidator,
    IValidator<ListContractDiscountRequestDto> listContractDiscountRequestValidator) : Controller
{
    [RequirePermission(Permission.ContractTermsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<ContractDiscountResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateContractDiscount(CreateContractDiscountRequestDto createContractDiscountRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createContractDiscountRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createContractDiscountRequestValidator.ValidateAndThrowAsync(createContractDiscountRequestDto, HttpContext.RequestAborted);

        var contractDiscount = await contractDiscountService.CreateAsync(
            createContractDiscountRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, contractDiscount.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ContractTermsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ContractDiscountResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListContractDiscounts([FromQuery] ListContractDiscountRequestDto listContractDiscountRequestDto)
    {
        await listContractDiscountRequestValidator.ValidateAndThrowAsync(listContractDiscountRequestDto, HttpContext.RequestAborted);

        var contractDiscounts = await contractDiscountService.ListAsync(
            listContractDiscountRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(contractDiscounts.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.ContractTermsRead)]
    [HttpGet("{ContractDiscountId:guid}")]
    [ProducesResponseType<ResponseDataDto<ContractDiscountResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractDiscount(ContractDiscountRequestDto contractDiscountRequestDto)
    {
        await contractDiscountRequestValidator.ValidateAndThrowAsync(contractDiscountRequestDto, HttpContext.RequestAborted);

        var contractDiscount = await contractDiscountService.GetAsync(
            new ContractDiscountId(contractDiscountRequestDto.ContractDiscountId), HttpContext.RequestAborted);

        return Ok(contractDiscount.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ContractTermsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{ContractDiscountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContractDiscount(DeleteContractDiscountRequestDto deleteContractDiscountRequestDto)
    {
        await deleteContractDiscountRequestValidator.ValidateAndThrowAsync(deleteContractDiscountRequestDto, HttpContext.RequestAborted);

        await contractDiscountService.DeleteAsync(
            new ContractDiscountId(deleteContractDiscountRequestDto.ContractDiscountId), HttpContext.RequestAborted);

        return NoContent();
    }
}
