using Core.Models.Dunning;
using Core.Models.Payments;
using Core.ValueObjects;
using Presentation.Models.Dunning;

namespace Presentation.Mappers.Dunning;

public static class DunningDtoMapper
{
    public static ListDunningStateRequest ToModel(this ListDunningStateRequestDto dto)
    {
        return new ListDunningStateRequest
        {
            OrganizationId = dto.FilterOrganizationId.HasValue
                ? new OrganizationId(dto.FilterOrganizationId.Value)
                : null,
            IncludeResolved = new IncludeResolved(dto.IncludeResolved ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static DunningStateResponseDto ToDto(this DunningState dunningState, InvoiceSettlement? invoiceSettlement)
    {
        return new DunningStateResponseDto
        {
            DunningStateId = dunningState.DunningStateId,
            InvoiceId = dunningState.InvoiceId,
            InvoiceNumber = dunningState.Invoice?.InvoiceNumber?.ToString(),
            OrganizationId = dunningState.OrganizationId,
            Stage = dunningState.Stage.ToString(),
            OutstandingAmount = invoiceSettlement?.OutstandingAmount ?? 0m,
            Currency = dunningState.Invoice?.Currency.ToString(),
            DueAt = dunningState.Invoice?.DueAt.HasValue == true ? dunningState.Invoice.DueAt.Value : null,
            NextAttemptAt = dunningState.NextAttemptAt,
            LastNoticedAt = dunningState.LastNoticedAt.HasValue ? dunningState.LastNoticedAt.Value : null,
            CollectionAttempts = dunningState.CollectionAttempts,
            LastAttemptedAt = dunningState.LastAttemptedAt.HasValue ? dunningState.LastAttemptedAt.Value : null,
            RestrictionProposedAt = dunningState.RestrictionProposedAt.HasValue
                ? dunningState.RestrictionProposedAt.Value
                : null,
            ResolvedAt = dunningState.ResolvedAt.HasValue ? dunningState.ResolvedAt.Value : null,
            CreatedAt = dunningState.CreatedAt
        };
    }
}
