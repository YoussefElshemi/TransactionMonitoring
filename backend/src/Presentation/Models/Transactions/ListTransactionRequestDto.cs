using Presentation.Models.Common;

namespace Presentation.Models.Transactions;

public record ListTransactionRequestDto : PagedRequestDto
{
    public string? FriendlyName { get; init; }
    public string? ExternalReference { get; init; }
}
