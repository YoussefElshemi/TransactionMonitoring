using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.SuspiciousActivityReports;
using Presentation.Models.SuspiciousActivityReports;
using Presentation.Validators.Common;

namespace Presentation.Validators.SuspiciousActivityReports;

public class ListSuspiciousActivityReportRequestValidator(
    ISuspiciousActivityReportService suspiciousActivityReportService,
    TimeProvider timeProvider) : PagedRequestValidator<ListSuspiciousActivityReportRequestDto>(timeProvider)
{
    // Must run before the page count rule, which Enum.Parses this string.
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Status)
            .IsEnumName(typeof(SuspiciousActivityReportStatus), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }

    protected override Task<TotalCount> GetCountAsync(ListSuspiciousActivityReportRequestDto request, CancellationToken stoppingToken)
    {
        return suspiciousActivityReportService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
