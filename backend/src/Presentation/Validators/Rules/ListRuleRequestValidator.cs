using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Rules;
using Presentation.Models.Rules;
using Presentation.Validators.Common;

namespace Presentation.Validators.Rules;

public class ListRuleRequestValidator(
    IRuleSetService ruleSetService,
    IRuleService ruleService,
    TimeProvider timeProvider) : PagedRequestValidator<ListRuleRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.RuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.RuleSetId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Set not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListRuleRequestDto request, CancellationToken stoppingToken)
    {
        return ruleService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}