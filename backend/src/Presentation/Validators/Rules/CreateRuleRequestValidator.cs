using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class CreateRuleRequestValidator : AbstractValidator<CreateRuleRequestDto>
{
    public const int MaxRulesInRuleSet = 20;

    public CreateRuleRequestValidator(IRuleSetService ruleSetService, IRuleService ruleService)
    {
        RuleFor(x => x.Body.FriendlyName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Body.RuleBody)
            .NotEmpty()
            .SetValidator(new RuleBodyValidator());

        RuleFor(x => x.Body.IsEnabled)
            .NotNull();

        RuleFor(x => x.Body.MatchPolarity)
            .IsEnumName(typeof(RuleMatchPolarity), false)
            .WithMessage($"MatchPolarity must be one of: {string.Join(", ", Enum.GetNames<RuleMatchPolarity>())}");

        RuleFor(x => x.Body.Action)
            .IsEnumName(typeof(RuleAction), false)
            .WithMessage($"Action must be one of: {string.Join(", ", Enum.GetNames<RuleAction>())}");

        RuleFor(x => x.Body.Severity)
            .IsEnumName(typeof(RuleSeverity), false)
            .WithMessage($"Severity must be one of: {string.Join(", ", Enum.GetNames<RuleSeverity>())}");

        RuleFor(x => x.Body.RiskWeight)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.Body.HoldWindow)
            .GreaterThan(0)
            .When(x => x.Body.HoldWindow.HasValue);

        RuleFor(x => x.Body.HoldExpiryVerdict)
            .IsEnumName(typeof(HoldExpiryVerdict), false)
            .WithMessage($"HoldExpiryVerdict must be one of: {string.Join(", ", Enum.GetNames<HoldExpiryVerdict>())}")
            .When(x => x.Body.HoldExpiryVerdict != null);

        RuleFor(x => x.Body.AlertSubjectRole)
            .IsEnumName(typeof(PartyRole), false)
            .WithMessage($"AlertSubjectRole must be one of: {string.Join(", ", Enum.GetNames<PartyRole>())}")
            .When(x => x.Body.AlertSubjectRole != null);

        RuleFor(x => x.Body.DeduplicationWindow)
            .GreaterThan(0)
            .When(x => x.Body.DeduplicationWindow.HasValue);

        RuleFor(x => x.RuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.RuleSetId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Set not found")
            .MustAsync(async (x, _, ct) =>
            {
                var listRuleRequest = new ListRuleRequest
                {
                    PageNumber = new PageNumber(1),
                    PageSize = new PageSize(1),
                    OrganizationId = new OrganizationId(x.OrganizationId),
                    RuleSetId = new RuleSetId(x.RuleSetId),
                    FromDate = new DateRange(DateOnly.MinValue),
                    ToDate = new DateRange(DateOnly.MaxValue),
                    FriendlyName = null
                };

                return await ruleService.GetCountAsync(listRuleRequest, ct) < MaxRulesInRuleSet;
            })
            .WithMessage($"{nameof(RuleSet)} count cannot be greater than {MaxRulesInRuleSet}");
    }
}