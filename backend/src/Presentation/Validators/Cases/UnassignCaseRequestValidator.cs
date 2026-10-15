using System.Net;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Cases;

namespace Presentation.Validators.Cases;

public class UnassignCaseRequestValidator : AbstractValidator<UnassignCaseRequestDto>
{
    public UnassignCaseRequestValidator(ICaseService caseService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found")
            .MustAsync(async (x, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x), ct);
                return @case.AssignedToUserId.HasValue && !@case.Status.IsTerminal();
            })
            .WithMessage($"{nameof(Case)} must be assigned and still open");
    }
}