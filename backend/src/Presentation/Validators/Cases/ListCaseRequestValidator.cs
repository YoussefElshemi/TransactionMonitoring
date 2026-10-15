using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Cases;
using Presentation.Models.Cases;
using Presentation.Validators.Common;

namespace Presentation.Validators.Cases;

public class ListCaseRequestValidator(
    ICaseService caseService,
    IOrganizationUserService organizationUserService,
    TimeProvider timeProvider) : PagedRequestValidator<ListCaseRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Status)
            .IsEnumName(typeof(CaseStatus), false)
            .When(x => x.Status != null);

        RuleFor(x => x.Query)
            .Must(SearchQuery.IsValid)
            .WithMessage($"A query is between {SearchQuery.MinimumLength} and {SearchQuery.MaximumLength} characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Query));

        RuleFor(x => x.Disposition)
            .IsEnumName(typeof(CaseDisposition), false)
            .When(x => x.Disposition != null);

        RuleFor(x => x.Severity)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => x.Severity != null);

        RuleFor(x => x.AssignedToUserId)
            .MustAsync(async (x, _, ct) =>
                await organizationUserService.ExistsAsync(
                    new OrganizationId(x.OrganizationId),
                    new ApplicationUserId(x.AssignedToUserId!.Value),
                    new IncludeDeleted(false),
                    ct))
            .WithMessage($"User was not found in {nameof(Organization)}")
            .When(x => x.AssignedToUserId != null);
    }

    protected override Task<TotalCount> GetCountAsync(ListCaseRequestDto request, CancellationToken stoppingToken)
    {
        return caseService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}