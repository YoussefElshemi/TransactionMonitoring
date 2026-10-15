using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Revenue;

public record ListRecurringRevenueRequestDto
{
    [FromQuery]
    public required int FromMonth { get; init; }

    [FromQuery]
    public required int FromYear { get; init; }

    [FromQuery]
    public required int ToMonth { get; init; }

    [FromQuery]
    public required int ToYear { get; init; }
}

public record GetRevenueConcentrationRequestDto
{
    [FromQuery]
    public required int Month { get; init; }

    [FromQuery]
    public required int Year { get; init; }

    [FromQuery]
    public int TopCount { get; init; } = 20;
}

public record ListCustomerMarginRequestDto
{
    [FromQuery]
    public required int Month { get; init; }

    [FromQuery]
    public required int Year { get; init; }
}

public record GetRecoveryStatisticRequestDto
{
    [FromQuery]
    public required DateTimeOffset PeriodStart { get; init; }

    [FromQuery]
    public required DateTimeOffset PeriodEnd { get; init; }
}

public record ListCustomerHealthRequestDto
{
    [FromQuery]
    public int WindowDays { get; init; } = 30;
}

public record ExportRevenueRecognitionRequestDto
{
    [FromQuery]
    public required int FromMonth { get; init; }

    [FromQuery]
    public required int FromYear { get; init; }

    [FromQuery]
    public required int ToMonth { get; init; }

    [FromQuery]
    public required int ToYear { get; init; }
}
