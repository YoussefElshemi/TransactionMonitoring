using System.Reflection;
using Core.Enums;
using Core.Exceptions;
using Core.Models.PartyMappings;
using Core.Models.Roles;
using Core.Models.TransactionSchemas;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.ExceptionHandlers;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.ExceptionHandlers;

public class ProblemDetailsExtensionCoverageTests : BaseTestClass
{
    private static readonly IReadOnlyList<DomainException> Samples =
    [
        new DecisionAlreadyRecordedException(new TransactionId(Guid.NewGuid())),
        new InvitationAlreadyMemberException(true),
        new InvitationNotPendingException(InvitationStatus.Accepted),
        new NotAvailableInLiveModeException("sandbox:reset"),
        new NotAvailableInTestModeException("suspicious-activity-reports:file"),
        new NotReplayableException("Email"),
        new PartyMappingConflictException(new OrganizationId(Guid.NewGuid()), new InvalidOperationException()),
        new PartyMappingInUseException([CreatePartyMappingUsage()]),
        new PermissionDeniedException(Permission.CasesRead, "queue"),
        new PermissionNotGrantableException([Permission.PlatformAdminister], true),
        new RoleInUseException(CreateRoleUsage()),
        new SandboxCreditsExhaustedException(
            new SandboxCredits(1000), new Count(1000), new Count(1), new ResetAt(TestTimeConstants.Today)),
        new SandboxTriggerException(),

        // Stands in for the abstract SchemaViolationException, whose override all six subclasses inherit.
        new PayloadSchemaViolationException([new SchemaViolation { Field = "amount", Message = "required" }]),
        new TransactionPayloadMismatchException(new ExternalReference("PAY-1")),
        new TransactionSchemaConflictException(new OrganizationId(Guid.NewGuid()), new InvalidOperationException()),
        new VelocityProfileInUseException([CreateVelocityProfileUsage()])
    ];

    [Fact]
    public void NoExtension_ShadowsAProblemDetailsMember()
    {
        // Arrange
        var colliding = Samples
            .SelectMany(exception => ExtensionNames(exception).Select(name => (Exception: exception, Name: name)))
            .Where(x => DomainExceptionHandler.ReservedMembers.Contains(x.Name))
            .Select(x => $"{x.Exception.GetType().Name}.{x.Name}")
            .OrderBy(x => x)
            .ToList();

        // Act
        colliding.Should().BeEmpty(
            "an extension named after an RFC 7807 member is dropped before it reaches the response, so a " +
            "client reading it reads the member instead. Rename: {0}",
            string.Join(", ", colliding));
    }

    [Fact]
    public void EveryExceptionThatDeclaresExtensions_HasASample()
    {
        // Arrange
        var sampled = Samples.Select(x => DeclaringType(x.GetType())).ToHashSet();

        var unsampled = DeclaredExtensionTypes()
            .Where(x => !sampled.Contains(x))
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToList();

        // Act
        unsampled.Should().BeEmpty(
            "every exception overriding GetExtensions needs one instance here, or its extension names are " +
            "never checked. Missing: {0}",
            string.Join(", ", unsampled));
    }

    [Fact]
    public void EverySample_ActuallyReturnsExtensions()
    {
        // Arrange
        var empty = Samples
            .Where(x => x.GetExtensions() is null || ExtensionNames(x).Count == 0)
            .Select(x => x.GetType().Name)
            .ToList();

        // Act
        // Guards the guard: a null payload passes the collision check over nothing.
        empty.Should().BeEmpty();
    }

    [Fact]
    public void TheSuite_ActuallyFoundTheExceptions()
    {
        // Arrange
        var count = DeclaredExtensionTypes().Count;

        // Assert
        // Guards the guard: an empty reflection result would pass every assertion above.
        count.Should().BeGreaterThan(10);
    }

    private static IReadOnlyList<string> ExtensionNames(DomainException exception)
    {
        var extensions = exception.GetExtensions();

        if (extensions is null)
        {
            return [];
        }

        return extensions.GetType()
            .GetProperties()
            .Where(x => x.CanRead)
            .Select(x => DomainExceptionHandler.ToExtensionName(x.Name))
            .ToList();
    }

    private static List<Type> DeclaredExtensionTypes()
    {
        return typeof(DomainException).Assembly
            .GetTypes()
            .Where(x => typeof(DomainException).IsAssignableFrom(x))
            .Select(DeclaringType)
            .Where(x => x != typeof(DomainException))
            .Distinct()
            .ToList();
    }

    private static Type DeclaringType(Type exceptionType)
    {
        return exceptionType
            .GetMethod(nameof(DomainException.GetExtensions), BindingFlags.Public | BindingFlags.Instance)!
            .DeclaringType!;
    }

    private static PartyMappingUsage CreatePartyMappingUsage()
    {
        return new PartyMappingUsage
        {
            RuleId = new RuleId(Guid.NewGuid()),
            FriendlyName = new FriendlyName("Structuring"),
            IsEnabled = new IsEnabled(true),
            Roles = [PartyRole.Remitter]
        };
    }

    private static RoleUsage CreateRoleUsage()
    {
        return new RoleUsage
        {
            RoleId = new RoleId(Guid.NewGuid()),
            Name = new RoleName("Reviewer"),
            AssignmentCount = new TotalCount(1),
            UserIds = [new ApplicationUserId(Guid.NewGuid())]
        };
    }

    private static VelocityProfileUsage CreateVelocityProfileUsage()
    {
        return new VelocityProfileUsage
        {
            RuleId = new RuleId(Guid.NewGuid()),
            FriendlyName = new FriendlyName("Velocity"),
            IsEnabled = new IsEnabled(true),
            Metrics = [new VelocityMetricName("count")],
            Thresholds = [new VelocityThresholdName("daily")]
        };
    }
}
