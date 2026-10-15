using System.Diagnostics;
using System.Globalization;
using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Billing;
using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using PaymentMethodKind = Core.Enums.PaymentMethodKind;
using PaymentStatus = Core.Enums.PaymentStatus;

namespace Infrastructure.Services;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly PaymentConfig _paymentConfig;
    private readonly CustomerService _customerService;
    private readonly SessionService _sessionService;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly PaymentMethodService _paymentMethodService;

    public StripePaymentProvider(IOptions<AppConfig> appConfig)
    {
        _paymentConfig = appConfig.Value.PaymentConfig;

        // A client per instance, not the static key, which is global process state.
        var stripeClient = new StripeClient(_paymentConfig.ApiKey);

        _customerService = new CustomerService(stripeClient);
        _sessionService = new SessionService(stripeClient);
        _paymentIntentService = new PaymentIntentService(stripeClient);
        _paymentMethodService = new PaymentMethodService(stripeClient);
    }

    public async Task<ProviderCustomerReference> EnsureCustomerAsync(BillingProfile billingProfile, CancellationToken stoppingToken)
    {
        var customer = await _customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Name = billingProfile.LegalName,
                Email = billingProfile.ContactEmail,
                Address = new AddressOptions
                {
                    Line1 = FirstAddressLine(billingProfile.Address),
                    Country = billingProfile.CountryCode
                },
                Metadata = new Dictionary<string, string>
                {
                    ["organizationId"] = billingProfile.OrganizationId.ToString(),
                    ["billingProfileId"] = billingProfile.BillingProfileId.ToString()
                }
            },
            cancellationToken: stoppingToken);

        return new ProviderCustomerReference(customer.Id);
    }

    public async Task<HostedSessionUrl> CreateSessionAsync(ProviderSessionRequest providerSessionRequest, CancellationToken stoppingToken)
    {
        var session = await _sessionService.CreateAsync(
            new SessionCreateOptions
            {
                Mode = "setup",
                Customer = providerSessionRequest.CustomerReference,
                PaymentMethodTypes = ["card", "sepa_debit", "bacs_debit"],
                SuccessUrl = providerSessionRequest.ReturnUrl,
                CancelUrl = providerSessionRequest.CancelUrl
            },
            cancellationToken: stoppingToken);

        return new HostedSessionUrl(session.Url);
    }

    public async Task<List<ProviderPaymentMethod>> ListPaymentMethodsAsync(
        ProviderCustomerReference providerCustomerReference, CancellationToken stoppingToken)
    {
        var customer = await _customerService.GetAsync(
            providerCustomerReference,
            new CustomerGetOptions { Expand = ["invoice_settings.default_payment_method"] },
            cancellationToken: stoppingToken);

        var defaultReference = customer.InvoiceSettings?.DefaultPaymentMethodId;
        var paymentMethods = new List<ProviderPaymentMethod>();

        foreach (var type in new[] { "card", "sepa_debit", "bacs_debit" })
        {
            var page = await _paymentMethodService.ListAsync(
                new PaymentMethodListOptions
                {
                    Customer = providerCustomerReference,
                    Type = type
                },
                cancellationToken: stoppingToken);

            paymentMethods.AddRange(page.Data.Select(x => Describe(x, defaultReference)));
        }

        if (paymentMethods.Count == 1)
        {
            paymentMethods[0] = paymentMethods[0] with { IsDefault = new IsDefault(true) };
        }

        return paymentMethods;
    }

    public async Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest providerChargeRequest, CancellationToken stoppingToken)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Customer = providerChargeRequest.CustomerReference,
                    PaymentMethod = providerChargeRequest.PaymentMethodReference?.ToString(),
                    Amount = ToMinorUnits(providerChargeRequest.Amount, providerChargeRequest.Currency),
                    Currency = providerChargeRequest.Currency.ToString().ToLowerInvariant(),

                    OffSession = true,
                    Confirm = true,

                    Description = providerChargeRequest.InvoiceNumber.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        ["invoiceNumber"] = providerChargeRequest.InvoiceNumber.ToString()
                    }
                },
                new RequestOptions { IdempotencyKey = providerChargeRequest.IdempotencyKey },
                stoppingToken);

            return Succeeded(paymentIntent);
        }
        catch (StripeException exception)
        {
            Activity.Current?.AddException(exception);

            return Declined(exception);
        }
    }

    public ProviderEvent? ReadEvent(string payload, string? signature)
    {
        if (signature is null || _paymentConfig.WebhookSecret is null)
        {
            return null;
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _paymentConfig.WebhookSecret,
                _paymentConfig.WebhookToleranceSeconds);

            return Translate(stripeEvent, payload);
        }
        catch (StripeException exception)
        {
            Activity.Current?.AddException(exception);

            return null;
        }
    }

    private static ProviderEvent Translate(Event stripeEvent, string payload)
    {
        (PaymentStatus? PaymentStatus, PaymentIntent? PaymentIntent) translated = stripeEvent.Type switch
        {
            EventTypes.PaymentIntentSucceeded => (PaymentStatus.Settled, stripeEvent.Data.Object as PaymentIntent),

            EventTypes.PaymentIntentProcessing => (PaymentStatus.Pending, stripeEvent.Data.Object as PaymentIntent),

            EventTypes.PaymentIntentPaymentFailed => (PaymentStatus.Failed, stripeEvent.Data.Object as PaymentIntent),

            EventTypes.ChargeDisputeCreated => (PaymentStatus.Returned, PaymentIntentOf(stripeEvent)),
            EventTypes.ChargeRefunded => (PaymentStatus.Returned, PaymentIntentOf(stripeEvent)),

            _ => (null, null)
        };

        var paymentIntent = translated.PaymentIntent;

        return new ProviderEvent
        {
            EventReference = new ProviderEventReference(stripeEvent.Id),
            EventType = new ProviderEventType(stripeEvent.Type),
            Payload = new Payload(payload),
            PaymentReference = paymentIntent is null ? null : new ProviderPaymentReference(paymentIntent.Id),
            PaymentStatus = paymentIntent is null ? null : translated.PaymentStatus,
            PaymentMethodReference = paymentIntent?.PaymentMethodId is null
                ? null
                : new PaymentMethodReference(paymentIntent.PaymentMethodId),
            PaymentMethodLabel = null,
            PaymentMethodKind = null,
            FailureCode = paymentIntent?.LastPaymentError?.Code is null
                ? null
                : new FailureCode(paymentIntent.LastPaymentError.Code),
            FailureReason = paymentIntent?.LastPaymentError?.Message is null
                ? null
                : new FailureReason(paymentIntent.LastPaymentError.Message)
        };
    }

    private static PaymentIntent? PaymentIntentOf(Event stripeEvent)
    {
        var paymentIntentId = stripeEvent.Data.Object switch
        {
            Charge charge => charge.PaymentIntentId,
            Dispute dispute => dispute.PaymentIntentId,
            _ => null
        };

        return paymentIntentId is null ? null : new PaymentIntent { Id = paymentIntentId };
    }

    private static ProviderChargeResult Succeeded(PaymentIntent paymentIntent)
    {
        return new ProviderChargeResult
        {
            PaymentReference = new ProviderPaymentReference(paymentIntent.Id),
            PaymentStatus = ToPaymentStatus(paymentIntent.Status),
            PaymentMethodReference = paymentIntent.PaymentMethodId is null
                ? null
                : new PaymentMethodReference(paymentIntent.PaymentMethodId),
            PaymentMethodLabel = null,
            PaymentMethodKind = null,
            FailureCode = null,
            FailureReason = null
        };
    }

    private static ProviderChargeResult Declined(StripeException exception)
    {
        return new ProviderChargeResult
        {
            PaymentReference = exception.StripeError?.PaymentIntent?.Id is null
                ? null
                : new ProviderPaymentReference(exception.StripeError.PaymentIntent.Id),
            PaymentStatus = PaymentStatus.Failed,
            PaymentMethodReference = null,
            PaymentMethodLabel = null,
            PaymentMethodKind = null,
            FailureCode = exception.StripeError?.Code is null ? null : new FailureCode(exception.StripeError.Code),
            FailureReason = new FailureReason(exception.StripeError?.Message ?? exception.Message)
        };
    }

    private static PaymentStatus ToPaymentStatus(string status)
    {
        return status switch
        {
            "succeeded" => PaymentStatus.Settled,
            "processing" => PaymentStatus.Pending,
            _ => PaymentStatus.Failed
        };
    }

    private static ProviderPaymentMethod Describe(PaymentMethod paymentMethod, string? defaultReference)
    {
        var (kind, label) = paymentMethod.Type switch
        {
            "sepa_debit" => (PaymentMethodKind.DirectDebit, $"SEPA ···· {paymentMethod.SepaDebit?.Last4}"),
            "bacs_debit" => (PaymentMethodKind.DirectDebit, $"Bacs ···· {paymentMethod.BacsDebit?.Last4}"),
            _ => (PaymentMethodKind.Card, $"{Capitalise(paymentMethod.Card?.Brand)} ···· {paymentMethod.Card?.Last4}")
        };

        return new ProviderPaymentMethod
        {
            PaymentMethodReference = new PaymentMethodReference(paymentMethod.Id),
            PaymentMethodKind = kind,
            Label = new PaymentMethodLabel(label),

            MandateReference = paymentMethod.SepaDebit is null && paymentMethod.BacsDebit is null
                ? null
                : new MandateReference(paymentMethod.Id),
            IsDefault = new IsDefault(paymentMethod.Id == defaultReference),

            ExpiresAt = paymentMethod.Card is null
                ? null
                : new ExpiresAt(new DateTimeOffset(
                    (int)paymentMethod.Card.ExpYear, (int)paymentMethod.Card.ExpMonth, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1))
        };
    }

    // Minor units, rounded away from zero to match the invoice's rounding.
    private static long ToMinorUnits(decimal amount, Currency currency)
    {
        return (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    }

    private static string FirstAddressLine(PostalAddress postalAddress)
    {
        var lines = ((string)postalAddress).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines.Length > 0 ? lines[0] : string.Empty;
    }

    private static string Capitalise(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
    }
}
