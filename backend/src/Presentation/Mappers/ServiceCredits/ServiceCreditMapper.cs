using Core.Models.ServiceCredits;
using Core.ValueObjects;
using Presentation.Models.ServiceCredits;

namespace Presentation.Mappers.ServiceCredits;

public static class ServiceCreditMapper
{
    public static IssueServiceCreditRequest ToModel(this IssueServiceCreditRequestDto dto)
    {
        return new IssueServiceCreditRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Month = new Month(dto.Body.Month),
            Year = new Year(dto.Body.Year)
        };
    }

    public static ListServiceCreditRequest ToModel(this ListServiceCreditRequestDto dto)
    {
        return new ListServiceCreditRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            IncludeMet = new IncludeMet(dto.IncludeMet ?? true),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static ServiceCreditResponseDto ToDto(this ServiceCredit serviceCredit)
    {
        return new ServiceCreditResponseDto
        {
            ServiceCreditId = serviceCredit.ServiceCreditId,
            OrganizationId = serviceCredit.OrganizationId,
            AvailabilityCommitmentId = serviceCredit.AvailabilityCommitmentId,
            Month = serviceCredit.Month,
            Year = serviceCredit.Year,
            AvailabilityTarget = serviceCredit.AvailabilityTarget,
            MeasuredUptimePercentage = serviceCredit.MeasuredUptimePercentage,
            TotalRequests = serviceCredit.TotalRequests,
            FailedRequests = serviceCredit.FailedRequests,
            CreditPercentage = serviceCredit.CreditPercentage,
            Amount = serviceCredit.Amount,
            Currency = serviceCredit.Currency.ToString(),
            InvoiceId = serviceCredit.InvoiceId.HasValue ? serviceCredit.InvoiceId.Value : null,
            CreditNoteId = serviceCredit.CreditNoteId.HasValue ? serviceCredit.CreditNoteId.Value : null,
            CreatedAt = serviceCredit.CreatedAt,
            UpdatedAt = serviceCredit.UpdatedAt
        };
    }
}
