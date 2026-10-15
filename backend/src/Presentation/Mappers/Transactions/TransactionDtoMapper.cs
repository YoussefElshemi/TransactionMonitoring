using System.Text.Json.Nodes;
using Core.Helpers;
using Core.Models.Transactions;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.Parties;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Models.Transactions;

namespace Presentation.Mappers.Transactions;

public static class TransactionDtoMapper
{
    public static TransactionResponseDto ToDto(this Transaction transaction)
    {
        return transaction.ToDto(PayloadMasker.Mask(transaction.Payload), true);
    }

    public static TransactionResponseDto ToRevealedDto(this Transaction transaction)
    {
        return transaction.ToDto(transaction.Payload, false);
    }

    public static TransactionSummaryResponseDto ToSummaryDto(this Transaction transaction)
    {
        return new TransactionSummaryResponseDto
        {
            Mode = transaction.Mode.ToString(),
            TransactionId = transaction.TransactionId,
            FriendlyName = transaction.FriendlyName,
            ExternalReference = transaction.ExternalReference?.ToString(),

            Payload = PayloadMasker.Mask(transaction.Payload),
            PayloadMasked = true,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    private static TransactionResponseDto ToDto(this Transaction transaction, JsonObject payload, bool payloadMasked)
    {
        return new TransactionResponseDto
        {
            Mode = transaction.Mode.ToString(),
            TransactionId = transaction.TransactionId,
            FriendlyName = transaction.FriendlyName,
            ExternalReference = transaction.ExternalReference?.ToString(),
            OrganizationId = transaction.OrganizationId,
            Payload = payload,
            PayloadMasked = payloadMasked,
            RuleEvaluations = transaction.RuleEvaluations.Select(x => x.ToDto()).ToArray(),
            Cases = transaction.Cases.Select(x => x.ToDto()).ToArray(),
            Parties = transaction.TransactionParties.Select(x => x.ToDto()).ToArray(),
            Decision = transaction.Decision?.ToDto(),
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
