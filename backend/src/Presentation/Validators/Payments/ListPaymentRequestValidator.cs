using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Payments;
using Presentation.Models.Payments;
using Presentation.Validators.Common;

namespace Presentation.Validators.Payments;

public class ListPaymentRequestValidator(
    IOrganizationService organizationService,
    IPaymentService paymentService,
    TimeProvider timeProvider) : PagedRequestValidator<ListPaymentRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.PaymentStatus)
            .Must(x => Enum.TryParse<PaymentStatus>(x, ignoreCase: true, out var paymentStatus) && Enum.IsDefined(paymentStatus))
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentStatus))
            .WithMessage("Payment status must be one of Pending, Settled, Failed, Returned");
    }

    protected override Task<TotalCount> GetCountAsync(ListPaymentRequestDto request, CancellationToken stoppingToken)
    {
        return paymentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
