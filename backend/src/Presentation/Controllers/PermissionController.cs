using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Permissions;
using Presentation.Models.Common;
using Presentation.Models.Permissions;

namespace Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[OrganizationScopeExempt(
    "The catalogue belongs to the installation and the grants belong to the caller. Neither reads a tenant, so whoever runs the installation can be answered from the default organisation.")]
[Route("permissions")]
public class PermissionController(IPermissionService permissionService) : Controller
{
    [RequirePermission(Permission.PermissionsRead)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<PermissionResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPermissions()
    {
        var effectivePermissions = await permissionService.ResolveAsync(User, HttpContext.RequestAborted);

        var permissions = Enum.GetValues<Permission>()
            .Select(x => x.ToDto(effectivePermissions))
            .OrderBy(x => x.Resource, StringComparer.Ordinal)
            .ThenBy(x => x.Action, StringComparer.Ordinal)
            .ToArray();

        return Ok(permissions.ToResponseData());
    }

    [HttpGet("mine")]
    [ProducesResponseType<ResponseDataDto<EffectivePermissionResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyPermissions()
    {
        var effectivePermissions = await permissionService.ResolveAsync(User, HttpContext.RequestAborted);

        var grants = effectivePermissions.Grants
            .Select(x => x.ToDto())
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();

        return Ok(grants.ToResponseData());
    }
}
