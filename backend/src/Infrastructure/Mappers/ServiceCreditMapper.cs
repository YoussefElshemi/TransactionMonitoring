using Core.Enums;
using Core.Models.ServiceCredits;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ServiceCreditMapper
{
    public static ServiceCredit ToModel(this ServiceCreditEntity serviceCreditEntity)
    {
        return new ServiceCredit
        {
            ServiceCreditId = new ServiceCreditId(serviceCreditEntity.ServiceCreditId),
            OrganizationId = new OrganizationId(serviceCreditEntity.OrganizationId),
            AvailabilityCommitmentId = new AvailabilityCommitmentId(serviceCreditEntity.AvailabilityCommitmentId),
            Month = new Month(serviceCreditEntity.Month),
            Year = new Year(serviceCreditEntity.Year),
            AvailabilityTarget = new AvailabilityTarget(serviceCreditEntity.AvailabilityTarget),
            MeasuredUptimePercentage = new UptimePercentage(serviceCreditEntity.MeasuredUptimePercentage),
            TotalRequests = new RequestCount(serviceCreditEntity.TotalRequests),
            FailedRequests = new RequestCount(serviceCreditEntity.FailedRequests),
            CreditPercentage = new CreditPercentage(serviceCreditEntity.CreditPercentage),
            Amount = new ServiceCreditAmount(serviceCreditEntity.Amount),
            Currency = (Currency)serviceCreditEntity.CurrencyId,
            InvoiceId = serviceCreditEntity.InvoiceId.HasValue ? new InvoiceId(serviceCreditEntity.InvoiceId.Value) : null,
            CreditNoteId = serviceCreditEntity.CreditNoteId.HasValue ? new CreditNoteId(serviceCreditEntity.CreditNoteId.Value) : null,
            CreatedAt = new CreatedAt(serviceCreditEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(serviceCreditEntity.UpdatedAt)
        };
    }

    public static ServiceCreditEntity ToEntity(this ServiceCredit serviceCredit)
    {
        return new ServiceCreditEntity
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
            CurrencyId = (int)serviceCredit.Currency,
            InvoiceId = serviceCredit.InvoiceId.HasValue ? serviceCredit.InvoiceId.Value : null,
            CreditNoteId = serviceCredit.CreditNoteId.HasValue ? serviceCredit.CreditNoteId.Value : null,
            CreatedAt = serviceCredit.CreatedAt,
            UpdatedAt = serviceCredit.UpdatedAt
        };
    }
}
