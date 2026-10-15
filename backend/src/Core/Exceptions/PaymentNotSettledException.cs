using System.Net;
using Core.Enums;
using Core.ValueObjects;

namespace Core.Exceptions;

public class PaymentNotSettledException(PaymentId paymentId, PaymentStatus paymentStatus)
    : DomainException($"Payment '{paymentId}' is {paymentStatus} and has no receipt. Only a settled payment has one.")
{
    public PaymentId PaymentId { get; } = paymentId;
    public PaymentStatus PaymentStatus { get; } = paymentStatus;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Payment not settled";
    }
}
