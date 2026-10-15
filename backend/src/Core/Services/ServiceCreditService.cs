using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.CreditNotes;
using Core.Models.ServiceCredits;
using Core.ValueObjects;

namespace Core.Services;

public class ServiceCreditService(
    IServiceCreditRepository serviceCreditRepository,
    IAvailabilityService availabilityService,
    IAvailabilityCommitmentService availabilityCommitmentService,
    IInvoiceService invoiceService,
    ICreditNoteService creditNoteService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IServiceCreditService
{
    public async Task<ServiceCredit> IssueAsync(IssueServiceCreditRequest issueServiceCreditRequest, CancellationToken stoppingToken)
    {
        var organizationId = issueServiceCreditRequest.OrganizationId;
        var month = issueServiceCreditRequest.Month;
        var year = issueServiceCreditRequest.Year;

        if (await serviceCreditRepository.ExistsByPeriodAsync(organizationId, month, year, stoppingToken))
        {
            throw new ServiceCreditAlreadyIssuedException(organizationId, month, year);
        }

        var periodEnd = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
        var commitment = await availabilityCommitmentService.GetInEffectAsync(
            organizationId, periodEnd.AddTicks(-1), stoppingToken);

        if (commitment is null)
        {
            throw new NoAvailabilityCommitmentException(organizationId, month, year);
        }

        var measurement = await availabilityService.MeasureMonthAsync(organizationId, month, year, stoppingToken);
        var creditPercentage = ServiceCreditCalculator.Resolve(commitment.CreditBands, measurement.UptimePercentage);

        var invoice = await invoiceService.GetIssuedByDateAsync(organizationId, month, year, stoppingToken);

        var amount = invoice is null
            ? new ServiceCreditAmount(0m)
            : ServiceCreditCalculator.Calculate(creditPercentage, invoice.NetAmount);

        var now = timeProvider.GetUtcNow();

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var creditNote = invoice is null || amount <= 0m
                ? null
                : await creditNoteService.CreateAsync(new CreateCreditNoteRequest
                {
                    OrganizationId = organizationId,
                    InvoiceId = invoice.InvoiceId,
                    Reason = new CreditNoteReason(
                        $"Service credit for {month}/{year}: availability {measurement.UptimePercentage} per cent against a target of {commitment.AvailabilityTarget} per cent."),
                    NetAmount = new NetAmount(amount),

                    CreatedByUserId = null
                }, stoppingToken);

            var serviceCredit = new ServiceCredit
            {
                ServiceCreditId = new ServiceCreditId(Guid.NewGuid()),
                OrganizationId = organizationId,
                AvailabilityCommitmentId = commitment.AvailabilityCommitmentId,
                Month = month,
                Year = year,
                AvailabilityTarget = commitment.AvailabilityTarget,
                MeasuredUptimePercentage = measurement.UptimePercentage,
                TotalRequests = measurement.TotalRequests,
                FailedRequests = measurement.FailedRequests,
                CreditPercentage = creditPercentage,
                Amount = amount,

                Currency = invoice?.Currency ?? Currency.Gbp,

                InvoiceId = invoice?.InvoiceId,
                CreditNoteId = creditNote?.CreditNoteId,
                CreatedAt = new CreatedAt(now),
                UpdatedAt = new UpdatedAt(now)
            };

            await serviceCreditRepository.AddAsync(serviceCredit, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            return serviceCredit;
        }, stoppingToken);
    }

    public Task<ServiceCredit> GetAsync(ServiceCreditId serviceCreditId, CancellationToken stoppingToken)
    {
        return serviceCreditRepository.GetAsync(serviceCreditId, stoppingToken);
    }

    public Task<Exists> ExistsByPeriodAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        return serviceCreditRepository.ExistsByPeriodAsync(organizationId, month, year, stoppingToken);
    }

    public Task<PagedResponse<ServiceCredit>> ListAsync(ListServiceCreditRequest listServiceCreditRequest, CancellationToken stoppingToken)
    {
        return serviceCreditRepository.ListAsync(listServiceCreditRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListServiceCreditRequest listServiceCreditRequest, CancellationToken stoppingToken)
    {
        return serviceCreditRepository.GetCountAsync(listServiceCreditRequest, stoppingToken);
    }
}
