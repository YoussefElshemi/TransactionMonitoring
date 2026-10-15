using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.CreditNotes;
using Presentation.Models.CreditNotes;
using Presentation.Validators.Common;

namespace Presentation.Validators.CreditNotes;

public class ListCreditNoteRequestValidator(
    IOrganizationService organizationService,
    ICreditNoteService creditNoteService,
    TimeProvider timeProvider) : PagedRequestValidator<ListCreditNoteRequestDto>(timeProvider)
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

    protected override Task<TotalCount> GetCountAsync(ListCreditNoteRequestDto request, CancellationToken stoppingToken)
    {
        return creditNoteService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
