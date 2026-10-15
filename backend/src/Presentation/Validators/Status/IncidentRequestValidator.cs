using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class IncidentRequestValidator : AbstractValidator<IncidentRequestDto>
{
    public IncidentRequestValidator(IIncidentService incidentService)
    {
        RuleFor(x => x.IncidentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await incidentService.ExistsAsync(new IncidentId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Incident not found");
    }
}
