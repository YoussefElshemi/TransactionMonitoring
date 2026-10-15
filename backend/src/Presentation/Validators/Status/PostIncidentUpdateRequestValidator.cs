using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class PostIncidentUpdateRequestValidator : AbstractValidator<PostIncidentUpdateRequestDto>
{
    public PostIncidentUpdateRequestValidator(IIncidentService incidentService)
    {
        RuleFor(x => x.IncidentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await incidentService.ExistsAsync(new IncidentId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Incident not found");

        RuleFor(x => x.Body.Status)
            .IsInEnum();

        RuleFor(x => x.Body.Body)
            .NotEmpty()
            .MaximumLength(IncidentUpdateBody.MaxLength);
    }
}
