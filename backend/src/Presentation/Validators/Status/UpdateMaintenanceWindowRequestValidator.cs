using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class UpdateMaintenanceWindowRequestValidator : AbstractValidator<UpdateMaintenanceWindowRequestDto>
{
    public UpdateMaintenanceWindowRequestValidator(IMaintenanceWindowService maintenanceWindowService, TimeProvider timeProvider)
    {
        RuleFor(x => x.MaintenanceWindowId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await maintenanceWindowService.ExistsAsync(new MaintenanceWindowId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Maintenance window not found");

        RuleFor(x => x.Body.Title)
            .NotEmpty()
            .MaximumLength(MaintenanceWindowTitle.MaxLength);

        RuleFor(x => x.Body.Description)
            .NotEmpty()
            .MaximumLength(MaintenanceWindowDescription.MaxLength);

        RuleFor(x => x.Body.Components)
            .NotEmpty()
            .WithMessage("A maintenance window must name at least one affected component");

        RuleForEach(x => x.Body.Components)
            .IsInEnum();

        RuleFor(x => x.Body.ScheduledStart)
            .GreaterThan(_ => timeProvider.GetUtcNow())
            .WithMessage("Maintenance must be announced before it starts");

        RuleFor(x => x.Body.ScheduledEnd)
            .GreaterThan(x => x.Body.ScheduledStart)
            .WithMessage("ScheduledEnd must be after ScheduledStart");
    }
}
