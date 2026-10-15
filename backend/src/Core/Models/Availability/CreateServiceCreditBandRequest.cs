using Core.ValueObjects;

namespace Core.Models.Availability;

public record CreateServiceCreditBandRequest
{
    public required UptimePercentage BelowUptimePercentage { get; init; }
    public required CreditPercentage CreditPercentage { get; init; }
}
