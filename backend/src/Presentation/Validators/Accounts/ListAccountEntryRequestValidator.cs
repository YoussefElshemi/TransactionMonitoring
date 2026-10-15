using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Accounts;
using Presentation.Models.Accounts;
using Presentation.Validators.Common;

namespace Presentation.Validators.Accounts;

public class ListAccountEntryRequestValidator(
    IOrganizationService organizationService,
    IAccountService accountService,
    TimeProvider timeProvider) : PagedRequestValidator<ListAccountEntryRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListAccountEntryRequestDto request, CancellationToken stoppingToken)
    {
        return accountService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
