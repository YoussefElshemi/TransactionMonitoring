using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class UpdateIncidentRequestValidator : AbstractValidator<UpdateIncidentRequestDto>
{
    public UpdateIncidentRequestValidator(IIncidentService incidentService, TimeProvider timeProvider)
    {
        RuleFor(x => x.IncidentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await incidentService.ExistsAsync(new IncidentId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Incident not found");

        RuleFor(x => x.Body.Title)
            .NotEmpty()
            .MaximumLength(IncidentTitle.MaxLength);

        RuleFor(x => x.Body.Summary)
            .NotEmpty()
            .MaximumLength(IncidentSummary.MaxLength);

        RuleFor(x => x.Body.Impact)
            .IsInEnum();

        RuleFor(x => x.Body.Components)
            .NotEmpty()
            .WithMessage("An incident must name at least one affected component");

        RuleForEach(x => x.Body.Components)
            .IsInEnum();

        RuleFor(x => x.Body.StartedAt)
            .LessThanOrEqualTo(_ => timeProvider.GetUtcNow())
            .WithMessage("StartedAt cannot be in the future");
    }
}
