using System.Net;
using Core.Exceptions;
using Core.Models.Errors;
using Core.ValueObjects;

namespace Core.Constants;

public static class ErrorCatalogue
{
    public static readonly IReadOnlyList<ErrorDefinition> General =
    [
        Define(ErrorCodes.ValidationFailed, HttpStatusCode.BadRequest,
            "One or more fields failed validation. The errors member names each field and what was wrong with it."),
        Define(ErrorCodes.BadRequest, HttpStatusCode.BadRequest,
            "The request could not be read: malformed JSON, a body that is too large, or a value of the wrong type."),
        Define(ErrorCodes.InvalidMode, HttpStatusCode.BadRequest,
            "The mode header was supplied but names neither the live nor the test partition."),
        Define(ErrorCodes.InvalidIdempotencyKey, HttpStatusCode.BadRequest,
            "The idempotency key header was supplied but is not a GUID."),
        Define(ErrorCodes.UnsupportedApiVersion, HttpStatusCode.BadRequest,
            "The API version header names a version this installation does not serve."),
        Define(ErrorCodes.Unauthorized, HttpStatusCode.Unauthorized,
            "Authentication is missing, expired or invalid."),
        Define(ErrorCodes.Forbidden, HttpStatusCode.Forbidden,
            "The caller is authenticated but may not perform this operation."),
        Define(ErrorCodes.ModeMismatch, HttpStatusCode.Forbidden,
            "The mode header names a partition the API client's key was not issued in. Live keys serve only live, test keys only test."),
        Define(ErrorCodes.NotFound, HttpStatusCode.NotFound,
            "The resource does not exist, or exists in a partition or organization the caller cannot see."),
        Define(ErrorCodes.Conflict, HttpStatusCode.Conflict,
            "The request is valid but cannot be applied to the resource in its current state."),
        Define(ErrorCodes.IdempotencyRequestInProgress, HttpStatusCode.Conflict,
            "An earlier request with the same idempotency key is still running. Retry once it has finished."),
        Define(ErrorCodes.IdempotencyKeyReused, HttpStatusCode.Conflict,
            "The idempotency key was already used for a different endpoint."),
        Define(ErrorCodes.Gone, HttpStatusCode.Gone,
            "The resource existed and has been permanently removed."),
        Define(ErrorCodes.UnprocessableEntity, HttpStatusCode.UnprocessableEntity,
            "The request was understood but its content cannot be acted on."),
        Define(ErrorCodes.RateLimited, HttpStatusCode.TooManyRequests,
            "The caller is over its request budget. Retry after the number of seconds in the Retry-After header."),
        Define(ErrorCodes.InternalError, HttpStatusCode.InternalServerError,
            "An unexpected error occurred. Quote the correlation id when reporting it."),
        Define(ErrorCodes.ServiceUnavailable, HttpStatusCode.ServiceUnavailable,
            "The service, or something it depends on, is temporarily unavailable."),
        Define(ErrorCodes.RequestFailed, HttpStatusCode.BadRequest,
            "The request was refused with a client error status that has no more specific code.")
    ];

    public static readonly IReadOnlyList<ErrorDefinition> Domain =
    [
        Define<AvailabilityCommitmentOverlapException>(HttpStatusCode.Conflict, "The availability commitment overlaps one already in force."),
        Define<BillingEntityInUseException>(HttpStatusCode.Conflict, "The billing entity has issued documents or has customers pointed at it, and cannot be removed."),
        Define<BuiltInRoleException>(HttpStatusCode.Conflict, "The role is built in and cannot be changed or removed."),
        Define<CommitmentInUseException>(HttpStatusCode.Conflict, "The commitment has been drawn against by an issued invoice and cannot be removed."),
        Define<CommitmentOverlapException>(HttpStatusCode.Conflict, "The commitment overlaps one already in force."),
        Define<CreditExceedsInvoiceException>(HttpStatusCode.Conflict, "The credit is larger than what remains of the invoice it is raised against."),
        Define<CrossOrganizationScopeException>(HttpStatusCode.BadRequest, "The scope names something outside the organization."),
        Define<CurrencyMismatchException>(HttpStatusCode.Conflict, "The invoice and the money offered against it are in different currencies."),
        Define<DecisionAlreadyRecordedException>(HttpStatusCode.Conflict, "The transaction has already been decided."),
        Define<IncidentNotResolvedException>(HttpStatusCode.Conflict, "The incident has not been resolved yet."),
        Define<IncidentResolvedException>(HttpStatusCode.Conflict, "The incident has already been resolved."),
        Define<InsufficientBalanceException>(HttpStatusCode.Conflict, "The account holds less than the amount requested."),
        Define<InvalidInvitationTokenException>(HttpStatusCode.BadRequest, "The invitation link is unknown, expired, revoked or already used."),
        Define<InvalidOffboardingException>(HttpStatusCode.BadRequest, "The offboarding request is not valid for this person or this successor."),
        Define<InvalidPriceStructureException>(HttpStatusCode.BadRequest, "The pricing on the plan version cannot be billed with."),
        Define<InvalidStatusSubscriptionTokenException>(HttpStatusCode.BadRequest, "The status subscription link is unknown or no longer valid."),
        Define<InvalidTaxIdentifierException>(HttpStatusCode.UnprocessableEntity, "The tax identifier is not the shape a registration takes in that country."),
        Define<InvalidTimeRangeException>(HttpStatusCode.BadRequest, "The time range is not usable."),
        Define<InvitationAlreadyMemberException>(HttpStatusCode.Conflict, "The address already belongs to this organization or to another one."),
        Define<InvitationNotPendingException>(HttpStatusCode.Conflict, "The invitation is no longer pending."),
        Define<InvoiceAdjustmentAppliedException>(HttpStatusCode.Conflict, "The adjustment has already been applied to an invoice."),
        Define<InvoiceIssuedException>(HttpStatusCode.Conflict, "The invoice has been issued and cannot be changed. Raise a credit note against it."),
        Define<InvoiceNotIssuedException>(HttpStatusCode.Conflict, "The invoice has not been issued yet."),
        Define<InvoiceSettledException>(HttpStatusCode.Conflict, "The invoice is already settled."),
        Define<MaintenanceWindowStartedException>(HttpStatusCode.Conflict, "The maintenance window has already started."),
        Define<MissingExchangeRateException>(HttpStatusCode.Conflict, "No exchange rate is in force for the currencies and date involved."),
        Define<NoAvailabilityCommitmentException>(HttpStatusCode.Conflict, "No availability commitment covers the period."),
        Define<NoBillingProfileException>(HttpStatusCode.Conflict, "The organization has no billing profile."),
        Define<NoPaymentMethodException>(HttpStatusCode.Conflict, "The organization has no payment method on file."),
        Define<NoPlanInEffectException>(HttpStatusCode.Conflict, "The organization is on no plan for the period."),
        Define<NotAvailableInLiveModeException>(HttpStatusCode.Conflict, "The operation exists only in test mode."),
        Define<NotAvailableInTestModeException>(HttpStatusCode.Conflict, "The operation is refused in test mode."),
        Define<NotReplayableException>(HttpStatusCode.Conflict, "A notification on this channel cannot be replayed."),
        Define<PartyMappingConflictException>(HttpStatusCode.Conflict, "The party mapping was changed by somebody else at the same time."),
        Define<PartyMappingInUseException>(HttpStatusCode.Conflict, "The party mapping is still referenced by rules."),
        Define<PartyMappingSchemaViolationException>(HttpStatusCode.BadRequest, "The party mapping does not match the transaction schema."),
        Define<PartyRuleViolationException>(HttpStatusCode.BadRequest, "The rule does not match the party mapping."),
        Define<PayloadSchemaViolationException>(HttpStatusCode.BadRequest, "The transaction payload does not match the transaction schema."),
        Define<PaymentNotSettledException>(HttpStatusCode.Conflict, "The payment has not settled."),
        Define<PaymentProviderUnavailableException>(HttpStatusCode.ServiceUnavailable, "No payment provider is configured on this installation."),
        Define<PermissionDeniedException>(HttpStatusCode.Forbidden, "The caller lacks the permission, or holds it for a different scope."),
        Define<PermissionNotGrantableException>(HttpStatusCode.Forbidden, "The caller may not grant a permission named in the request."),
        Define<PlanInUseException>(HttpStatusCode.Conflict, "The plan has subscriptions against it and cannot be removed."),
        Define<RoleInUseException>(HttpStatusCode.Conflict, "The role is still assigned."),
        Define<RuleSchemaViolationException>(HttpStatusCode.BadRequest, "The rule does not match the transaction schema."),
        Define<SandboxBusyException>(HttpStatusCode.Conflict, "A payment simulation is still running, so the sandbox cannot be reset yet."),
        Define<SandboxCreditsExhaustedException>(HttpStatusCode.PaymentRequired, "The sandbox allowance for the month has been spent."),
        Define<SandboxTriggerException>(HttpStatusCode.InternalServerError, "A failure forced by a sandbox trigger value."),
        Define<ServiceCreditAlreadyIssuedException>(HttpStatusCode.Conflict, "A service credit has already been issued for the period."),
        Define<SubscriptionTimelineException>(HttpStatusCode.Conflict, "The subscription change conflicts with the existing timeline."),
        Define<TransactionPayloadMismatchException>(HttpStatusCode.BadRequest, "The transaction reference already names a payment with a different payload."),
        Define<TransactionSchemaConflictException>(HttpStatusCode.Conflict, "The transaction schema was changed by somebody else at the same time."),
        Define<VelocityProfileInUseException>(HttpStatusCode.Conflict, "The velocity profile is still referenced by rules."),
        Define<VelocityProfileSchemaViolationException>(HttpStatusCode.BadRequest, "The velocity profile does not match the transaction schema."),
        Define<VelocityRuleViolationException>(HttpStatusCode.BadRequest, "The rule does not match the velocity profile.")
    ];

    public static readonly IReadOnlyList<ErrorDefinition> Errors = [.. General, .. Domain];

    private static ErrorDefinition Define<TException>(HttpStatusCode statusCode, string description)
        where TException : DomainException
    {
        return Define(DomainException.ToCode(typeof(TException)), statusCode, description);
    }

    private static ErrorDefinition Define(string code, HttpStatusCode statusCode, string description)
    {
        return new ErrorDefinition
        {
            Code = new ErrorCode(code),
            Status = new StatusCode((int)statusCode),
            Description = new ErrorDescription(description)
        };
    }
}
