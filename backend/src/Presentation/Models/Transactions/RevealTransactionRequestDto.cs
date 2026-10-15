using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Transactions;

// Separate type so it gets its own validator.
public record RevealTransactionRequestDto : TransactionRequestDto
{
    [FromBody]
    public required RevealTransactionBodyDto Body { get; init; }
}

public record RevealTransactionBodyDto
{
    public required string Reason { get; init; }
}
