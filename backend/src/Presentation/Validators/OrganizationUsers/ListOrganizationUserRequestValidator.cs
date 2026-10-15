using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.OrganizationUsers;
using Presentation.Models.OrganizationUsers;
using Presentation.Validators.Common;

namespace Presentation.Validators.OrganizationUsers;

public class ListOrganizationUserRequestValidator(
    IOrganizationUserService organizationUserService,
    IOrganizationService organizationService,
    TimeProvider timeProvider) : PagedRequestValidator<ListOrganizationUserRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListOrganizationUserRequestDto request, CancellationToken stoppingToken)
    {
        return organizationUserService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}