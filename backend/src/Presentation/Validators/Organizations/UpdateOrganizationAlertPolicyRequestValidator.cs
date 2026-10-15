using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Organizations;

namespace Presentation.Validators.Organizations;

public class UpdateOrganizationAlertPolicyRequestValidator : AbstractValidator<UpdateOrganizationAlertPolicyRequestDto>
{
    public UpdateOrganizationAlertPolicyRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.CaseGroupingPeriod)
            .Must(caseGroupingPeriod => caseGroupingPeriod.Value > 0)
            .WithMessage("'Case Grouping Period' must be greater than '0'.")
            .When(x => x.Body.CaseGroupingPeriod is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.DeduplicationWindow)
            .Must(deduplicationWindow => deduplicationWindow.Value > 0)
            .WithMessage("'Deduplication Window' must be greater than '0'.")
            .When(x => x.Body.DeduplicationWindow is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.MaxAlertsPerCase)
            .Must(maxAlertsPerCase => maxAlertsPerCase.Value > 0)
            .WithMessage("'Max Alerts Per Case' must be greater than '0'.")
            .When(x => x.Body.MaxAlertsPerCase is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.CaseGroupingScope)
            .Must(scope => Enum.GetNames<CaseGroupingScope>().Contains(scope.Value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"CaseGroupingScope must be one of: {string.Join(", ", Enum.GetNames<CaseGroupingScope>())}")
            .When(x => x.Body.CaseGroupingScope is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.CaseGroupingSeverityPolicy)
            .Must(policy => Enum.GetNames<CaseGroupingSeverityPolicy>().Contains(policy.Value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"CaseGroupingSeverityPolicy must be one of: {string.Join(", ", Enum.GetNames<CaseGroupingSeverityPolicy>())}")
            .When(x => x.Body.CaseGroupingSeverityPolicy is { IsSet: true, Value: not null });
    }
}
