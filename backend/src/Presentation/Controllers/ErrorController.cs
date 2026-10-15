using Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Mappers.Common;
using Presentation.Mappers.Errors;
using Presentation.Models.Common;
using Presentation.Models.Errors;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[AllowAnonymous]
[Route("errors")]
public class ErrorController : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<ErrorDefinitionResponseDto[]>>(StatusCodes.Status200OK)]
    public IActionResult ListErrors()
    {
        var errors = ErrorCatalogue.Errors
            .Select(x => x.ToDto())
            .ToArray();

        return Ok(errors.ToResponseData());
    }
}
