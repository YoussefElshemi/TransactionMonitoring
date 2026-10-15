using System.Runtime.CompilerServices;
using Core.Constants;
using Core.Exceptions;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Constants;

public class ErrorCatalogueTests : BaseTestClass
{
    // Written out, not read from the catalogue, so a type rename can't silently change a published code. Add entries; never edit or remove shipped ones (override GetCode instead).
    private static readonly string[] PublishedDomainCodes =
    [
        "availability_commitment_overlap",
        "billing_entity_in_use",
        "built_in_role",
        "commitment_in_use",
        "commitment_overlap",
        "credit_exceeds_invoice",
        "cross_organization_scope",
        "currency_mismatch",
        "decision_already_recorded",
        "incident_not_resolved",
        "incident_resolved",
        "insufficient_balance",
        "invalid_invitation_token",
        "invalid_offboarding",
        "invalid_price_structure",
        "invalid_status_subscription_token",
        "invalid_tax_identifier",
        "invalid_time_range",
        "invitation_already_member",
        "invitation_not_pending",
        "invoice_adjustment_applied",
        "invoice_issued",
        "invoice_not_issued",
        "invoice_settled",
        "maintenance_window_started",
        "missing_exchange_rate",
        "no_availability_commitment",
        "no_billing_profile",
        "no_payment_method",
        "no_plan_in_effect",
        "not_available_in_live_mode",
        "not_available_in_test_mode",
        "not_replayable",
        "party_mapping_conflict",
        "party_mapping_in_use",
        "party_mapping_schema_violation",
        "party_rule_violation",
        "payload_schema_violation",
        "payment_not_settled",
        "payment_provider_unavailable",
        "permission_denied",
        "permission_not_grantable",
        "plan_in_use",
        "role_in_use",
        "rule_schema_violation",
        "sandbox_busy",
        "sandbox_credits_exhausted",
        "sandbox_trigger",
        "service_credit_already_issued",
        "subscription_timeline",
        "transaction_payload_mismatch",
        "transaction_schema_conflict",
        "velocity_profile_in_use",
        "velocity_profile_schema_violation",
        "velocity_rule_violation"
    ];

    private static readonly string[] PublishedGeneralCodes =
    [
        "validation_failed",
        "bad_request",
        "invalid_mode",
        "invalid_idempotency_key",
        "unsupported_api_version",
        "unauthorized",
        "forbidden",
        "mode_mismatch",
        "not_found",
        "conflict",
        "idempotency_request_in_progress",
        "idempotency_key_reused",
        "gone",
        "unprocessable_entity",
        "rate_limited",
        "internal_error",
        "service_unavailable",
        "request_failed"
    ];

    [Fact]
    public void EveryDomainException_RaisesACodeThatWasPublished()
    {
        // Arrange
        var raised = ConcreteDomainExceptions()
            .Select(x => (Type: x, Code: Instantiate(x).GetCode()))
            .ToList();

        // Act
        var unpublished = raised
            .Where(x => !PublishedDomainCodes.Contains(x.Code))
            .Select(x => $"{x.Type.Name} -> {x.Code}")
            .Order()
            .ToList();

        // Assert
        unpublished.Should().BeEmpty(
            "a code is a published contract. A new exception adds its code to this list and to " +
            "ErrorCatalogue; a renamed one overrides GetCode to keep the code it already shipped with. " +
            "Unpublished: {0}",
            string.Join(", ", unpublished));
    }

    [Fact]
    public void EveryPublishedDomainCode_IsStillRaised()
    {
        // Arrange
        var raised = ConcreteDomainExceptions()
            .Select(x => Instantiate(x).GetCode())
            .ToHashSet();

        // Act
        var orphaned = PublishedDomainCodes.Where(x => !raised.Contains(x)).ToList();

        // Assert
        orphaned.Should().BeEmpty(
            "a published code nothing raises any more means an exception was renamed or deleted, and a " +
            "client branching on it now never matches. Orphaned: {0}",
            string.Join(", ", orphaned));
    }

    [Fact]
    public void TheCatalogue_ListsExactlyThePublishedCodes()
    {
        // Arrange
        var expected = PublishedGeneralCodes.Concat(PublishedDomainCodes);

        // Act
        var listed = ErrorCatalogue.Errors.Select(x => x.Code.ToString());

        // Assert
        listed.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TheCatalogue_ListsNoCodeTwice()
    {
        // Arrange
        var codes = ErrorCatalogue.Errors.Select(x => x.Code.ToString());

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void TheCatalogue_StatesTheStatusEachDomainExceptionActuallyReturns()
    {
        // Arrange
        var documented = ErrorCatalogue.Domain.ToDictionary(x => x.Code.ToString(), x => (int)x.Status);

        // Act
        var wrong = ConcreteDomainExceptions()
            .Select(Instantiate)
            .Where(x => documented[x.GetCode()] != (int)x.GetStatusCode())
            .Select(x => $"{x.GetCode()}: documented {documented[x.GetCode()]}, returns {(int)x.GetStatusCode()}")
            .ToList();

        // Assert
        wrong.Should().BeEmpty();
    }

    [Fact]
    public void TheSuite_ActuallyFoundTheExceptions()
    {
        // Arrange
        var count = ConcreteDomainExceptions().Count;

        // Assert
        // Guards the guard: an empty reflection result would pass every assertion above.
        count.Should().BeGreaterThan(50);
    }

    [Theory]
    [InlineData(typeof(InvoiceIssuedException), "invoice_issued")]
    [InlineData(typeof(NoPlanInEffectException), "no_plan_in_effect")]
    [InlineData(typeof(NotAvailableInLiveModeException), "not_available_in_live_mode")]
    public void ToCode_GivenAnExceptionType_SnakeCasesItsNameWithoutTheSuffix(Type exceptionType, string expected)
    {
        // Arrange
        var code = DomainException.ToCode(exceptionType);

        // Assert
        code.Should().Be(expected);
    }

    private static List<Type> ConcreteDomainExceptions()
    {
        return typeof(DomainException).Assembly
            .GetTypes()
            .Where(x => typeof(DomainException).IsAssignableFrom(x) && !x.IsAbstract)
            .ToList();
    }

    private static DomainException Instantiate(Type exceptionType)
    {
        return (DomainException)RuntimeHelpers.GetUninitializedObject(exceptionType);
    }
}
