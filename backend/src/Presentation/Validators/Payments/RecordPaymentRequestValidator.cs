using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Payments;

namespace Presentation.Validators.Payments;

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequestDto>
{
    public RecordPaymentRequestValidator(
        IOrganizationService organizationService,
        IInvoiceService invoiceService,
        TimeProvider timeProvider)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Amount)
            .GreaterThan(0)
            .WithMessage("A payment must be for more than nothing.");

        RuleFor(x => x.Body.Currency)
            .Must(x => Enum.TryParse<Currency>(x, ignoreCase: true, out var currency) && Enum.IsDefined(currency))
            .WithMessage("Currency must be one of Gbp, Eur, Usd");

        RuleFor(x => x.Body.ReceivedAt)
            .LessThanOrEqualTo(_ => timeProvider.GetUtcNow())
            .WithMessage("Received At cannot be in the future.");

        RuleFor(x => x.Body.InvoiceId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, invoiceId, ct) => await invoiceService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new InvoiceId(invoiceId!.Value), new IncludeDeleted(false), ct))
            .When(x => x.Body.InvoiceId.HasValue)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invoice not found");
    }
}
