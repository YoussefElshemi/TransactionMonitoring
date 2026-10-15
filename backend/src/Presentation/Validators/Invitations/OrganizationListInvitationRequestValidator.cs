using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Invitations;
using Presentation.Models.Invitations;
using Presentation.Validators.Common;

namespace Presentation.Validators.Invitations;

public class OrganizationListInvitationRequestValidator(
    IInvitationService invitationService,
    IOrganizationService organizationService,
    TimeProvider timeProvider) : PagedRequestValidator<OrganizationListInvitationRequestDto>(timeProvider)
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

    protected override Task<TotalCount> GetCountAsync(OrganizationListInvitationRequestDto request, CancellationToken stoppingToken)
    {
        return invitationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
