using Core.Configs;
using Core.Documents;
using Core.Enums;
using Core.Exceptions;
using Core.Factories;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Common;
using Core.Models.CreditNotes;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace Core.Services;

public class CreditNoteService(
    ICreditNoteRepository creditNoteRepository,
    IInvoiceService invoiceService,
    IBillingEntityService billingEntityService,
    IDocumentNumberRepository documentNumberRepository,
    IDisplayPreferenceService displayPreferenceService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext,
    IOptions<AppConfig> appConfig) : ICreditNoteService
{
    public async Task<CreditNote> CreateAsync(CreateCreditNoteRequest createCreditNoteRequest, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var invoice = await invoiceService.GetAsync(createCreditNoteRequest.InvoiceId, stoppingToken);

            if (invoice.InvoiceStatus is not InvoiceStatus.Issued || invoice.BillingEntityId is null)
            {
                throw new InvoiceNotIssuedException(invoice.InvoiceId);
            }

            var alreadyCredited = await creditNoteRepository.GetCreditedNetAsync(invoice.InvoiceId, stoppingToken);
            var remaining = new NetAmount(invoice.NetAmount - alreadyCredited);
            var netAmount = createCreditNoteRequest.NetAmount ?? remaining;

            if (netAmount <= 0 || netAmount > remaining)
            {
                throw new CreditExceedsInvoiceException(invoice.InvoiceId, netAmount, remaining);
            }

            var mirrorLines = alreadyCredited == 0 && netAmount == invoice.NetAmount;

            var billingEntity = await billingEntityService.GetAsync(invoice.BillingEntityId.Value, stoppingToken);
            var creditNoteId = new CreditNoteId(Guid.NewGuid());
            var issuedYear = new Year(timeProvider.GetUtcNow().Year);

            var position = await documentNumberRepository.NextCreditNotePositionAsync(
                billingEntity.BillingEntityId, issuedYear, stoppingToken);

            var creditNote = CreditNoteFactory.Create(
                createCreditNoteRequest,
                invoice,
                creditNoteId,
                DocumentNumberFormatter.CreditNote(billingEntity.CreditNoteNumberPrefix, issuedYear, position),
                netAmount,
                mirrorLines,
                timeProvider);

            await creditNoteRepository.AddAsync(creditNote, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            return await GetAsync(creditNoteId, stoppingToken);
        }, stoppingToken);
    }

    public Task<CreditNote> GetAsync(CreditNoteId creditNoteId, CancellationToken stoppingToken)
    {
        return creditNoteRepository.GetAsync(creditNoteId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CreditNoteId creditNoteId, CancellationToken stoppingToken)
    {
        return creditNoteRepository.ExistsAsync(organizationId, creditNoteId, stoppingToken);
    }

    public async Task<ExportedCreditNote> ExportAsync(CreditNoteId creditNoteId, CancellationToken stoppingToken)
    {
        using var live = modeContext.Pin(Mode.Live);

        var creditNote = await GetAsync(creditNoteId, stoppingToken);
        var document = new CreditNoteDocument(
            creditNote,
            appConfig.Value.InvoiceConfig,
            new Translator(displayPreferenceService.Resolve(creditNote.Locale, null, null)));

        return new ExportedCreditNote
        {
            CreditNote = creditNote,
            Content = document.GeneratePdf()
        };
    }

    public Task<PagedResponse<CreditNote>> ListAsync(ListCreditNoteRequest listCreditNoteRequest, CancellationToken stoppingToken)
    {
        return creditNoteRepository.ListAsync(listCreditNoteRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListCreditNoteRequest listCreditNoteRequest, CancellationToken stoppingToken)
    {
        return creditNoteRepository.GetCountAsync(listCreditNoteRequest, stoppingToken);
    }
}
