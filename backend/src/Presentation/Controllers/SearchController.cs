using Core.Enums;
using Core.Exceptions;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Common;
using Presentation.Mappers.Search;
using Presentation.Models.Common;
using Presentation.Models.Search;

namespace Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("search")]
public class SearchController(
    IApplicationUserService applicationUserService,
    IPermissionService permissionService,
    ISearchService searchService,
    PartyResponseFactory partyResponseFactory,
    CaseResponseFactory caseResponseFactory,
    IValidator<SearchRequestDto> searchRequestValidator) : Controller
{
    private static readonly IReadOnlyDictionary<SearchResultType, Permission> PermissionByType =
        new Dictionary<SearchResultType, Permission>
        {
            [SearchResultType.Party] = Permission.PartiesRead,
            [SearchResultType.Case] = Permission.CasesRead
        };

    [HttpGet]
    [ProducesResponseType<ResponseDataDto<SearchResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchRequestDto searchRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        searchRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await searchRequestValidator.ValidateAndThrowAsync(searchRequestDto, HttpContext.RequestAborted);

        var effectivePermissions = await permissionService.ResolveAsync(User, HttpContext.RequestAborted);

        var requested = searchRequestDto.ToTypes();
        var readable = requested.Where(x => effectivePermissions.Holds(PermissionByType[x])).ToList();

        if (readable.Count == 0)
        {
            throw new PermissionDeniedException(PermissionByType[requested[0]]);
        }

        var casePermissionFilter = readable.Contains(SearchResultType.Case)
            ? await permissionService.ResolveFilterAsync(User, Permission.CasesRead, HttpContext.RequestAborted)
            : null;

        var results = await searchService.SearchAsync(
            searchRequestDto.ToModel(readable, casePermissionFilter), HttpContext.RequestAborted);

        var enrichmentContext = authenticatedUser.ToEnrichmentContext(User);

        var response = new SearchResponseDto
        {
            Query = results.Query,
            Parties = results.Parties?.ToSection(
                await partyResponseFactory.CreateAsync(results.Parties.Data, enrichmentContext, HttpContext.RequestAborted)),
            Cases = results.Cases?.ToSection(
                await caseResponseFactory.CreateAsync(results.Cases.Data, enrichmentContext, HttpContext.RequestAborted))
        };

        return Ok(response.ToResponseData());
    }
}
