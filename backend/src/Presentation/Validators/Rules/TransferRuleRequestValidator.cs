using System.Net;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class TransferRuleRequestValidator : AbstractValidator<TransferRuleRequestDto>
{
    public TransferRuleRequestValidator(IRuleService ruleService, IRuleSetService ruleSetService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found");

        RuleFor(x => x.Body.TargetRuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.Body.TargetRuleSetId), new IncludeDeleted(false), ct))
            .WithMessage($"Target {nameof(RuleSet)} does not exist")
            .MustAsync(async (x, _, ct) =>
            {
                var listRuleRequest = new ListRuleRequest
                {
                    PageNumber = new PageNumber(1),
                    PageSize = new PageSize(1),
                    OrganizationId = new OrganizationId(x.OrganizationId),
                    RuleSetId = new RuleSetId(x.Body.TargetRuleSetId),
                    FromDate = new DateRange(DateOnly.MinValue),
                    ToDate = new DateRange(DateOnly.MaxValue),
                    FriendlyName = null
                };

                return await ruleService.GetCountAsync(listRuleRequest, ct) < CreateRuleRequestValidator.MaxRulesInRuleSet;
            })
            .WithMessage($"{nameof(RuleSet)} count cannot be greater than {CreateRuleRequestValidator.MaxRulesInRuleSet}");
    }
}