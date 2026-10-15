using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Mappers.ApiVersions;
using Presentation.Mappers.Common;
using Presentation.Middlewares;
using Presentation.Models.ApiVersions;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[AllowAnonymous]
[Route(ApiVersionMiddleware.CataloguePath)]
public class ApiVersionController(IApiVersionService apiVersionService) : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<ApiVersionCatalogueResponseDto>>(StatusCodes.Status200OK)]
    public IActionResult GetCatalogue()
    {
        return Ok(apiVersionService.GetCatalogue().ToDto().ToResponseData());
    }
}
