namespace Presentation.Models.Payments;

public record PaymentMethodSessionResponseDto
{
    public required string Url { get; init; }
}
