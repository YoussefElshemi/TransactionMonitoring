using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class SandboxCreditsExhaustedException(SandboxCredits allowance, Count used, Count requested, ResetAt resetsAt)
    : DomainException($"The sandbox allowance of {allowance} for this month has been spent.")
{
    public SandboxCredits Allowance { get; } = allowance;

    public Count Used { get; } = used;

    public Count Requested { get; } = requested;

    public ResetAt ResetsAt { get; } = resetsAt;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.PaymentRequired;
    }

    public override string GetTitle()
    {
        return "Sandbox allowance spent";
    }

    public override object GetExtensions()
    {
        return new
        {
            Allowance = (int)Allowance,
            Used = (int)Used,
            Remaining = Math.Max(0, Allowance - Used),
            Requested = (int)Requested,
            ResetsAt = ((DateTimeOffset)ResetsAt).ToString("O"),
            Mode = "Test"
        };
    }
}
