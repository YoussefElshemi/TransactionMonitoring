using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Payments;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PaymentRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListPaymentRequest, PaymentEntity, Payment>, IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken stoppingToken)
    {
        await context.Payments.AddAsync(payment.ToEntity(), stoppingToken);
    }

    public async Task<Payment> GetAsync(PaymentId paymentId, CancellationToken stoppingToken)
    {
        var paymentEntity = await context.Payments
            .Include(x => x.Organization)
            .FirstAsync(x => x.PaymentId == paymentId && !x.IsDeleted, stoppingToken);

        return paymentEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, PaymentId paymentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Payments
            .AnyAsync(x => x.PaymentId == paymentId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || !x.IsDeleted), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Payment?> FindByProviderReferenceAsync(ProviderPaymentReference providerPaymentReference, CancellationToken stoppingToken)
    {
        var paymentEntity = await context.Payments
            .FirstOrDefaultAsync(x => x.ProviderPaymentReference == (string)providerPaymentReference && !x.IsDeleted, stoppingToken);

        return paymentEntity?.ToModel();
    }

    public async Task<Attempts> GetAttemptCountAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var attempts = await context.Payments
            .CountAsync(x => x.RaisedForInvoiceId == (Guid)invoiceId && !x.IsDeleted, stoppingToken);

        return new Attempts(attempts);
    }

    public async Task SettleAsync(PaymentId paymentId, SettledAt settledAt, ReceiptNumber receiptNumber, CancellationToken stoppingToken)
    {
        var paymentEntity = await LoadAsync(paymentId, stoppingToken);

        paymentEntity.PaymentStatusId = (int)PaymentStatus.Settled;
        paymentEntity.SettledAt = settledAt;
        paymentEntity.ReceiptNumber = receiptNumber;

        paymentEntity.FailureCode = null;
        paymentEntity.FailureReason = null;
        paymentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Payments.Update(paymentEntity);
    }

    public async Task FailAsync(PaymentId paymentId, FailureCode? failureCode, FailureReason failureReason, CancellationToken stoppingToken)
    {
        var paymentEntity = await LoadAsync(paymentId, stoppingToken);

        paymentEntity.PaymentStatusId = (int)PaymentStatus.Failed;
        paymentEntity.FailureCode = failureCode?.ToString();
        paymentEntity.FailureReason = failureReason;
        paymentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Payments.Update(paymentEntity);
    }

    public async Task ReturnAsync(
        PaymentId paymentId, ReturnedAt returnedAt, FailureCode? failureCode, FailureReason failureReason, CancellationToken stoppingToken)
    {
        var paymentEntity = await LoadAsync(paymentId, stoppingToken);

        paymentEntity.PaymentStatusId = (int)PaymentStatus.Returned;
        paymentEntity.ReturnedAt = returnedAt;
        paymentEntity.FailureCode = failureCode?.ToString();
        paymentEntity.FailureReason = failureReason;
        paymentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Payments.Update(paymentEntity);
    }

    public async Task RecordMethodAsync(
        PaymentId paymentId,
        PaymentMethodKind paymentMethodKind,
        PaymentMethodReference? paymentMethodReference,
        PaymentMethodLabel? paymentMethodLabel,
        CancellationToken stoppingToken)
    {
        var paymentEntity = await LoadAsync(paymentId, stoppingToken);

        paymentEntity.PaymentMethodKindId = (int)paymentMethodKind;
        paymentEntity.PaymentMethodReference = paymentMethodReference?.ToString();
        paymentEntity.PaymentMethodLabel = paymentMethodLabel?.ToString();
        paymentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Payments.Update(paymentEntity);
    }

    private Task<PaymentEntity> LoadAsync(PaymentId paymentId, CancellationToken stoppingToken)
    {
        return context.Payments.FirstAsync(x => x.PaymentId == paymentId && !x.IsDeleted, stoppingToken);
    }

    protected override Task<List<Payment>> MapPageAsync(List<PaymentEntity> items, ListPaymentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<PaymentEntity> OrderByExpression()
    {
        return new OrderByExpression<PaymentEntity, DateTimeOffset>(x => x.ReceivedAt);
    }

    protected override IQueryable<PaymentEntity> FilterByPagedRequest(ListPaymentRequest listPaymentRequest)
    {
        var queryable = context.Payments
            .Where(x => x.OrganizationId == listPaymentRequest.OrganizationId && !x.IsDeleted);

        if (listPaymentRequest.PaymentStatus.HasValue)
        {
            queryable = queryable.Where(x => x.PaymentStatusId == (int)listPaymentRequest.PaymentStatus.Value);
        }

        return queryable;
    }

    protected override IQueryable<PaymentEntity> FilterByDateRange(IQueryable<PaymentEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.ReceivedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.ReceivedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
