using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Payments;

public record ListPaymentRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required PaymentStatus? PaymentStatus { get; init; }
}
