using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Alerts;

namespace Presentation.Validators.Alerts;

public class AlertRequestValidator : AbstractValidator<AlertRequestDto>
{
    public AlertRequestValidator(IAlertService alertService)
    {
        RuleFor(x => x.AlertId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await alertService.ExistsAsync(new OrganizationId(x.OrganizationId), new AlertId(x.AlertId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Alert not found");
    }
}
