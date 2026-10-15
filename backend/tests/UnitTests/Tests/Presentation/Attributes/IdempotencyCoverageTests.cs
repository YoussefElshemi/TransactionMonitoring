using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Presentation.Attributes;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Attributes;

public class IdempotencyCoverageTests : BaseTestClass
{
    private static readonly string[] MutatingMethods = ["POST", "PUT", "PATCH", "DELETE"];

    [Fact]
    public void EveryMutatingEndpoint_IsIdempotentOrExemptWithAReason()
    {
        // Arrange
        var undeclared = MutatingActions()
            .Where(x => !HasAttribute<IdempotentAttribute>(x) && !HasAttribute<IdempotencyExemptAttribute>(x))
            .Select(Describe)
            .OrderBy(x => x)
            .ToList();

        // Act
        undeclared.Should().BeEmpty(
            "every mutating endpoint must carry [Idempotent], or [IdempotencyExempt] stating why not. Missing: {0}",
            string.Join(", ", undeclared));
    }

    [Fact]
    public void EveryExemption_GivesANonEmptyReason()
    {
        // Arrange
        var unexplained = MutatingActions()
            .Select(x => (Action: x, Exemption: FindAttribute<IdempotencyExemptAttribute>(x)))
            .Where(x => x.Exemption != null && string.IsNullOrWhiteSpace(x.Exemption.Reason))
            .Select(x => Describe(x.Action))
            .ToList();

        // Act
        unexplained.Should().BeEmpty();
    }

    [Fact]
    public void NoEndpoint_IsBothIdempotentAndExempt()
    {
        // Arrange
        var contradictory = MutatingActions()
            .Where(x => HasAttribute<IdempotentAttribute>(x) && HasAttribute<IdempotencyExemptAttribute>(x))
            .Select(Describe)
            .ToList();

        // Act
        contradictory.Should().BeEmpty();
    }

    [Fact]
    public void TheSuite_ActuallyFoundTheControllers()
    {
        // Arrange
        var count = MutatingActions().Count();

        // Assert
        // Guards the guard: an empty reflection result would pass every assertion above.
        count.Should().BeGreaterThan(100);
    }

    private static IEnumerable<MethodInfo> MutatingActions()
    {
        return typeof(global::Presentation.Program).Assembly
            .GetTypes()
            .Where(x => typeof(ControllerBase).IsAssignableFrom(x) || typeof(Controller).IsAssignableFrom(x))
            .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(IsMutatingAction);
    }

    private static bool IsMutatingAction(MethodInfo method)
    {
        return method
            .GetCustomAttributes()
            .OfType<IActionHttpMethodProvider>()
            .SelectMany(x => x.HttpMethods)
            .Any(x => MutatingMethods.Contains(x, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasAttribute<T>(MethodInfo method) where T : Attribute
    {
        return FindAttribute<T>(method) != null;
    }

    private static T? FindAttribute<T>(MethodInfo method) where T : Attribute
    {
        return method.GetCustomAttribute<T>() ?? method.DeclaringType?.GetCustomAttribute<T>();
    }

    private static string Describe(MethodInfo method)
    {
        return $"{method.DeclaringType?.Name}.{method.Name}";
    }
}
