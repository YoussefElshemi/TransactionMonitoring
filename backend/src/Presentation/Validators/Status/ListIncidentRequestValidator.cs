using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Status;
using Presentation.Models.Status;
using Presentation.Validators.Common;

namespace Presentation.Validators.Status;

public class ListIncidentRequestValidator(
    IIncidentService incidentService,
    TimeProvider timeProvider) : PagedRequestValidator<ListIncidentRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Component)
            .IsInEnum()
            .When(x => x.Component.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }

    protected override Task<TotalCount> GetCountAsync(ListIncidentRequestDto request, CancellationToken stoppingToken)
    {
        return incidentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
