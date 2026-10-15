namespace Presentation.Models.Availability;

public record ServiceCreditBandResponseDto
{
    public required Guid ServiceCreditBandId { get; init; }
    public required decimal BelowUptimePercentage { get; init; }
    public required decimal CreditPercentage { get; init; }
}
