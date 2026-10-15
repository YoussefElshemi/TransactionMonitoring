using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Common;
using Core.Models.Dunning;
using Core.Models.Invoices;
using Core.Models.Payments;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Serilog;

namespace Core.Services;

public class DunningService(
    IDunningRepository dunningRepository,
    IInvoiceRepository invoiceRepository,
    IPaymentService paymentService,
    IBillingProfileService billingProfileService,
    IDisplayPreferenceService displayPreferenceService,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IModeContext modeContext,
    TimeProvider timeProvider,
    IOptions<AppConfig> appConfig,
    ILogger logger) : IDunningService
{
    public async Task<Count> ReconcileAsync(CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var overdueInvoices = await invoiceRepository.GetOverdueAsync(stoppingToken);
        var changed = 0;

        foreach (var invoice in overdueInvoices)
        {
            var settlement = await paymentService.GetSettlementAsync(invoice.InvoiceId, stoppingToken);
            var dunningState = await dunningRepository.FindOpenAsync(invoice.InvoiceId, stoppingToken);

            if (settlement.IsSettled)
            {
                if (dunningState is not null)
                {
                    await dunningRepository.ResolveAsync(
                        dunningState.DunningStateId, new ResolvedAt(timeProvider.GetUtcNow()), stoppingToken);

                    changed++;
                }

                continue;
            }

            if (dunningState is not null || invoice.DueAt is null)
            {
                continue;
            }

            await dunningRepository.AddAsync(Open(invoice), stoppingToken);
            changed++;
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(changed);
    }

    public async Task<Count> AdvanceAsync(CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var now = timeProvider.GetUtcNow();
        var paymentConfig = appConfig.Value.PaymentConfig;
        var dunningStates = await dunningRepository.ListDueAsync(new NextAttemptAt(now), stoppingToken);
        var advanced = 0;

        foreach (var dunningState in dunningStates)
        {
            var nextStage = DunningLadder.Next(dunningState.Stage);

            if (nextStage is null)
            {
                continue;
            }

            var invoice = await invoiceRepository.GetAsync(dunningState.InvoiceId, stoppingToken);
            var settlement = await paymentService.GetSettlementAsync(invoice.InvoiceId, stoppingToken);

            if (settlement.IsSettled)
            {
                await dunningRepository.ResolveAsync(
                    dunningState.DunningStateId, new ResolvedAt(now), stoppingToken);

                continue;
            }

            var collectionAttempts = dunningState.CollectionAttempts;
            var lastAttemptedAt = dunningState.LastAttemptedAt;

            if (DunningLadder.Collects(nextStage.Value) && paymentConfig.CollectionEnabled)
            {
                var attempted = await TryCollectAsync(invoice, stoppingToken);

                if (attempted)
                {
                    collectionAttempts += 1;
                    lastAttemptedAt = new LastAttemptedAt(now);
                }
            }

            await NotifyAsync(invoice, nextStage.Value, settlement, stoppingToken);

            await dunningRepository.AdvanceAsync(
                dunningState with
                {
                    Stage = nextStage.Value,
                    NextAttemptAt = new NextAttemptAt(DunningLadder.NextAttemptAt(nextStage.Value, now, paymentConfig)),
                    LastNoticedAt = new NoticedAt(now),
                    CollectionAttempts = collectionAttempts,
                    LastAttemptedAt = lastAttemptedAt,

                    RestrictionProposedAt = nextStage.Value is DunningStage.RestrictionProposed
                        ? new ProposedAt(now)
                        : dunningState.RestrictionProposedAt,
                    UpdatedAt = new UpdatedAt(now)
                },
                stoppingToken);

            advanced++;
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(advanced);
    }

    private async Task<bool> TryCollectAsync(Invoice invoice, CancellationToken stoppingToken)
    {
        try
        {
            await paymentService.CollectAsync(
                new CollectPaymentRequest
                {
                    InvoiceId = invoice.InvoiceId,
                    PaymentMethodReference = null
                },
                stoppingToken);

            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Warning(
                exception,
                "Collecting invoice {InvoiceNumber} during dunning failed: {Message}",
                invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString(),
                exception.Message);

            return false;
        }
    }

    private async Task NotifyAsync(
        Invoice invoice, DunningStage stage, InvoiceSettlement settlement, CancellationToken stoppingToken)
    {
        try
        {
            var billingProfile = await billingProfileService.FindAsync(invoice.OrganizationId, stoppingToken);

            if (billingProfile is null)
            {
                return;
            }

            var translator = new Translator(await displayPreferenceService.ForStatedAsync(
                billingProfile.Locale, invoice.OrganizationId, stoppingToken));

            var notificationMessage = DunningNotificationMessageBuilder.Build(
                invoice,
                settlement,
                stage,
                translator,
                timeProvider.GetUtcNow(),
                appConfig.Value.PaymentConfig.RestrictionProposalDays);

            await emailSender.SendAsync(new Destination(billingProfile.ContactEmail), notificationMessage, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Warning(
                exception,
                "Sending the {Stage} notice for invoice {InvoiceNumber} failed: {Message}",
                stage,
                invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString(),
                exception.Message);
        }
    }

    private DunningState Open(Invoice invoice)
    {
        var now = timeProvider.GetUtcNow();
        var (stage, nextAttemptAt) = DunningLadder.Open(invoice.DueAt!.Value, appConfig.Value.PaymentConfig);

        return new DunningState
        {
            DunningStateId = new DunningStateId(Guid.NewGuid()),
            InvoiceId = invoice.InvoiceId,
            Invoice = null,
            OrganizationId = invoice.OrganizationId,
            Stage = stage,
            NextAttemptAt = new NextAttemptAt(nextAttemptAt),

            LastNoticedAt = null,
            CollectionAttempts = new Attempts(0),
            LastAttemptedAt = null,
            RestrictionProposedAt = null,
            ResolvedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }

    public Task<DunningState> GetAsync(DunningStateId dunningStateId, CancellationToken stoppingToken)
    {
        return dunningRepository.GetAsync(dunningStateId, stoppingToken);
    }

    public Task<PagedResponse<DunningState>> ListAsync(ListDunningStateRequest listDunningStateRequest, CancellationToken stoppingToken)
    {
        return dunningRepository.ListAsync(listDunningStateRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListDunningStateRequest listDunningStateRequest, CancellationToken stoppingToken)
    {
        return dunningRepository.GetCountAsync(listDunningStateRequest, stoppingToken);
    }
}
