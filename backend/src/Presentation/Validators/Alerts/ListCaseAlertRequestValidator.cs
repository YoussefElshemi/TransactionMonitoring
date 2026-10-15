using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Alerts;
using Presentation.Models.Alerts;
using Presentation.Validators.Common;

namespace Presentation.Validators.Alerts;

public class ListCaseAlertRequestValidator(
    IAlertService alertService,
    ICaseService caseService,
    TimeProvider timeProvider) : PagedRequestValidator<ListCaseAlertRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Status)
            .IsEnumName(typeof(AlertStatus), false)
            .When(x => x.Status != null);

        RuleFor(x => x.Severity)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => x.Severity != null);

        RuleFor(x => x.RouteCaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.RouteCaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListCaseAlertRequestDto request, CancellationToken stoppingToken)
    {
        return alertService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
