using Core.Enums;
using Core.Models.Accounts;
using Core.Models.Payments;
using Core.ValueObjects;
using Presentation.Models.Accounts;

namespace Presentation.Mappers.Accounts;

public static class AccountDtoMapper
{
    public static ListAccountEntryRequest ToModel(this ListAccountEntryRequestDto dto)
    {
        return new ListAccountEntryRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static ListUnappliedReceiptRequest ToModel(this ListUnappliedReceiptRequestDto dto)
    {
        return new ListUnappliedReceiptRequest
        {
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static AllocatePaymentRequest ToModel(this AllocatePaymentRequestDto dto)
    {
        return new AllocatePaymentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            InvoiceId = new InvoiceId(dto.Body.InvoiceId),
            Amount = dto.Body.Amount.HasValue ? new EntryAmount(dto.Body.Amount.Value) : null,
            AllocatedByUserId = dto.AllocatedByUserId.HasValue ? new ApplicationUserId(dto.AllocatedByUserId.Value) : null
        };
    }

    public static RefundBalanceRequest ToModel(this RefundBalanceRequestDto dto)
    {
        return new RefundBalanceRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Currency = Enum.Parse<Currency>(dto.Body.Currency, ignoreCase: true),
            Amount = new EntryAmount(dto.Body.Amount),
            Reason = new EntryDescription(dto.Body.Reason),
            RefundedByUserId = dto.RefundedByUserId.HasValue ? new ApplicationUserId(dto.RefundedByUserId.Value) : null
        };
    }

    public static AccountEntryResponseDto ToDto(this AccountEntry accountEntry)
    {
        return new AccountEntryResponseDto
        {
            AccountEntryId = accountEntry.AccountEntryId,
            OrganizationId = accountEntry.OrganizationId,
            Currency = accountEntry.Currency.ToString(),
            Amount = accountEntry.Amount,
            Reason = accountEntry.Reason.ToString(),
            PaymentId = accountEntry.PaymentId.HasValue ? accountEntry.PaymentId.Value : null,
            InvoiceId = accountEntry.InvoiceId.HasValue ? accountEntry.InvoiceId.Value : null,
            CreditNoteId = accountEntry.CreditNoteId.HasValue ? accountEntry.CreditNoteId.Value : null,
            Description = accountEntry.Description,
            CreatedByUserId = accountEntry.CreatedByUserId.HasValue ? accountEntry.CreatedByUserId.Value : null,

            CreatedAt = accountEntry.CreatedAt
        };
    }

    public static AccountBalanceResponseDto ToDto(this AccountBalance accountBalance)
    {
        return new AccountBalanceResponseDto
        {
            OrganizationId = accountBalance.OrganizationId,
            Currency = accountBalance.Currency.ToString(),
            Amount = accountBalance.Amount,
            EntryCount = accountBalance.EntryCount
        };
    }
}
