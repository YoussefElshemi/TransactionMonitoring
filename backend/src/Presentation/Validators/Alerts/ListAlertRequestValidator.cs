using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Alerts;
using Presentation.Models.Alerts;
using Presentation.Validators.Common;

namespace Presentation.Validators.Alerts;

public class ListAlertRequestValidator(
    IAlertService alertService,
    TimeProvider timeProvider) : PagedRequestValidator<ListAlertRequestDto>(timeProvider)
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
    }

    protected override Task<TotalCount> GetCountAsync(ListAlertRequestDto request, CancellationToken stoppingToken)
    {
        return alertService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
