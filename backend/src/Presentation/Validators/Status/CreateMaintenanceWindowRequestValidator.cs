using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class CreateMaintenanceWindowRequestValidator : AbstractValidator<CreateMaintenanceWindowRequestDto>
{
    public CreateMaintenanceWindowRequestValidator(TimeProvider timeProvider)
    {
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
