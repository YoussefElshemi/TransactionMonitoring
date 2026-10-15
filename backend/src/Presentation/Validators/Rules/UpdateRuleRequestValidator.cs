using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class UpdateRuleRequestValidator : AbstractValidator<UpdateRuleRequestDto>
{
    public UpdateRuleRequestValidator(IRuleService ruleService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found");

        RuleFor(x => x.Body)
            .Must(x => x.FriendlyName != null || x.IsEnabled.HasValue || x.Action != null || x.Severity != null || x.RiskWeight.HasValue || x.HoldWindow.IsSet || x.HoldExpiryVerdict.IsSet || x.AlertSubjectRole.IsSet || x.DeduplicationWindow.IsSet)
            .WithMessage("At least one field must be provided");

        RuleFor(x => x.Body.FriendlyName)
            .MaximumLength(50)
            .When(x => x.Body.FriendlyName != null);

        RuleFor(x => x.Body.Action)
            .IsEnumName(typeof(RuleAction), false)
            .WithMessage($"Action must be one of: {string.Join(", ", Enum.GetNames<RuleAction>())}")
            .When(x => x.Body.Action != null);

        RuleFor(x => x.Body.Severity)
            .IsEnumName(typeof(RuleSeverity), false)
            .WithMessage($"Severity must be one of: {string.Join(", ", Enum.GetNames<RuleSeverity>())}")
            .When(x => x.Body.Severity != null);

        RuleFor(x => x.Body.RiskWeight)
            .InclusiveBetween(0, 100)
            .When(x => x.Body.RiskWeight.HasValue);

        RuleFor(x => x.Body.HoldWindow)
            .Must(holdWindow => holdWindow.Value > 0)
            .WithMessage("'Hold Window' must be greater than '0'.")
            .When(x => x.Body.HoldWindow is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.HoldExpiryVerdict)
            .Must(holdExpiryVerdict => Enum.GetNames<HoldExpiryVerdict>().Contains(holdExpiryVerdict.Value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"HoldExpiryVerdict must be one of: {string.Join(", ", Enum.GetNames<HoldExpiryVerdict>())}")
            .When(x => x.Body.HoldExpiryVerdict is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.AlertSubjectRole)
            .Must(alertSubjectRole => Enum.GetNames<PartyRole>().Contains(alertSubjectRole.Value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"AlertSubjectRole must be one of: {string.Join(", ", Enum.GetNames<PartyRole>())}")
            .When(x => x.Body.AlertSubjectRole is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.DeduplicationWindow)
            .Must(deduplicationWindow => deduplicationWindow.Value > 0)
            .WithMessage("'Deduplication Window' must be greater than '0'.")
            .When(x => x.Body.DeduplicationWindow is { IsSet: true, Value: not null });
    }
}
