using Core.Enums;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class ListMyQueueRequestValidator : AbstractValidator<ListMyQueueRequestDto>
{
    public ListMyQueueRequestValidator()
    {
        RuleFor(x => x.MemberUserId)
            .NotNull()
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .MaximumLength(QueueName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.RoutingPolicy)
            .IsEnumName(typeof(QueueRoutingPolicy), false)
            .When(x => !string.IsNullOrWhiteSpace(x.RoutingPolicy));
    }
}
