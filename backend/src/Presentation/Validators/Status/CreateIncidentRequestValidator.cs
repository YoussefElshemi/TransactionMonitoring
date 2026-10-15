using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class CreateIncidentRequestValidator : AbstractValidator<CreateIncidentRequestDto>
{
    public CreateIncidentRequestValidator(TimeProvider timeProvider)
    {
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
            .When(x => x.Body.StartedAt.HasValue)
            .WithMessage("StartedAt cannot be in the future");
    }
}
