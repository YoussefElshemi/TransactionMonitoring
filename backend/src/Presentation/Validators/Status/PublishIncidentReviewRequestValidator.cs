using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class PublishIncidentReviewRequestValidator : AbstractValidator<PublishIncidentReviewRequestDto>
{
    public PublishIncidentReviewRequestValidator(IIncidentService incidentService)
    {
        RuleFor(x => x.IncidentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await incidentService.ExistsAsync(new IncidentId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Incident not found");

        RuleFor(x => x.Body.RootCause)
            .NotEmpty()
            .MaximumLength(RootCause.MaxLength);

        RuleFor(x => x.Body.Remediation)
            .NotEmpty()
            .MaximumLength(Remediation.MaxLength);
    }
}
