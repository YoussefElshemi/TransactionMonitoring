using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BackTests;

namespace Presentation.Validators.BackTests;

public class CreateBackTestRequestValidator : AbstractValidator<CreateBackTestRequestDto>
{
    public CreateBackTestRequestValidator(
        IRuleService ruleService,
        IRuleVersionService ruleVersionService,
        IBackTestService backTestService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found")
            .MustAsync(async (x, _, ct) => !await backTestService.ExistsActiveAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), ct))
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("An active backTest already exists for this rule");

        When(x => x.Body is not null, () =>
        {
            RuleFor(x => x.Body!.SampleSize)
                .InclusiveBetween(0, 100)
                .WithMessage("Sample size must be between 0 and 100");
        });

        When(x => x.Body is not null && x.Body!.VersionNumber.HasValue, () =>
        {
            RuleFor(x => x)
                .MustAsync(async (x, _, ct) => await ruleVersionService.VersionExistsAsync(
                    new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new VersionNumber(x.Body!.VersionNumber!.Value), ct))
                .WithErrorCode(HttpStatusCode.NotFound.ToString())
                .WithMessage("Rule version not found");
        });
    }
}
