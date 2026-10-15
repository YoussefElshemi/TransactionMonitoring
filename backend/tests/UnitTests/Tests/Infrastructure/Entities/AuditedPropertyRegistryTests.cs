using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Entities;

public class AuditedPropertyRegistryTests
{
    public static TheoryData<IAuditable> Auditables()
    {
        var data = new TheoryData<IAuditable>();

        foreach (var auditable in CreateAuditables())
        {
            data.Add(auditable);
        }

        return data;
    }

    [Fact]
    public void For_EveryAuditableEntityType_ReturnsAtLeastOneProperty()
    {
        // Arrange
        var entityTypes = LiveEntityTypes();

        // Act
        var registrations = entityTypes.ToDictionary(x => x, AuditedPropertyRegistry.For);

        // Assert
        registrations.Should().NotBeEmpty();
        registrations.Should().OnlyContain(x => x.Value.Count > 0);
    }

    [Fact]
    public void EveryAuditableEntity_IsCoveredByADistinctEntityType()
    {
        // Arrange
        var auditables = CreateAuditables();

        // Act
        var entityTypes = auditables.Select(x => x.AuditEntityType).ToList();

        // Assert
        entityTypes.Should().OnlyHaveUniqueItems();
        entityTypes.Should().BeEquivalentTo(LiveEntityTypes());
    }

    [Theory]
    [MemberData(nameof(Auditables))]
    public void AuditedProperties_MatchTheRegistrationForTheEntityType(IAuditable auditable)
    {
        // Act
        var registered = AuditedPropertyRegistry.For(auditable.AuditEntityType);

        // Assert
        auditable.AuditedProperties.Should().BeSameAs(registered);
    }

    [Theory]
    [MemberData(nameof(Auditables))]
    public void AuditedProperties_OnlyNameRealPropertiesOfTheEntity(IAuditable auditable)
    {
        // Arrange
        var propertyNames = auditable.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => x.Name)
            .ToList();

        // Act
        var auditedNames = auditable.AuditedProperties.Select(x => x.Name).ToList();

        // Assert
        auditedNames.Should().OnlyHaveUniqueItems();
        auditedNames.Should().BeSubsetOf(propertyNames);
    }

    [Theory]
    [MemberData(nameof(Auditables))]
    public void AuditedProperties_NamingACollection_AreSnapshottedFromTheChangeTracker(IAuditable auditable)
    {
        // Arrange
        var collectionNames = auditable.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.PropertyType != typeof(string) && x.PropertyType.IsAssignableTo(typeof(IEnumerable)))
            .Select(x => x.Name)
            .ToHashSet();

        // Act
        var collectionProperties = auditable.AuditedProperties
            .Where(x => collectionNames.Contains(x.Name))
            .ToList();

        // Assert
        collectionProperties.Should().NotContain(x => x.SnapshotSelector == null);
    }

    [Fact]
    public void ApiClient_SnapshotsItsScopes()
    {
        // Act
        var scopes = AuditedPropertyRegistry.ApiClient.Single(x => x.Name == nameof(ApiClientEntity.Scopes));

        // Assert
        scopes.SnapshotSelector.Should().NotBeNull();
    }

    [Fact]
    public void ApplicationUser_SnapshotsItsRoles()
    {
        // Act
        var roles = AuditedPropertyRegistry.ApplicationUser.Single(x => x.Name == nameof(ApplicationUserEntity.Roles));

        // Assert
        roles.SnapshotSelector.Should().NotBeNull();
    }

    [Fact]
    public void WebhookConfiguration_DoesNotSnapshotTheSigningKey()
    {
        // Act
        var auditedNames = AuditedPropertyRegistry.WebhookConfiguration.Select(x => x.Name).ToList();

        // Assert
        auditedNames.Should().NotContain(nameof(WebhookConfigurationEntity.SecretKey));
        auditedNames.Should().Contain(nameof(WebhookConfigurationEntity.SecretKeyRotatedAt));
    }

    [Fact]
    public void ApiClientSecret_DoesNotSnapshotTheSecret()
    {
        // Act
        var auditedNames = AuditedPropertyRegistry.ApiClientSecret.Select(x => x.Name).ToList();

        // Assert
        auditedNames.Should().NotContain(nameof(ApiClientSecretEntity.SecretHash));
        auditedNames.Should().NotContain(nameof(ApiClientSecretEntity.SecretPrefix));
    }

    [Fact]
    public void ApplicationUser_DoesNotSnapshotIdentitySecrets()
    {
        // Arrange
        string[] secrets =
        [
            nameof(ApplicationUserEntity.PasswordHash),
            nameof(ApplicationUserEntity.SecurityStamp),
            nameof(ApplicationUserEntity.ConcurrencyStamp)
        ];

        // Act
        var auditedNames = AuditedPropertyRegistry.ApplicationUser.Select(x => x.Name);

        // Assert
        auditedNames.Should().NotIntersectWith(secrets);
    }

    private static List<AuditableEntityType> LiveEntityTypes()
    {
        return Enum.GetValues<AuditableEntityType>()
            .Except(AuditedPropertyRegistry.Retired)
            .ToList();
    }

    private static List<IAuditable> CreateAuditables()
    {
        // An uninitialised instance is enough: the audit members are constants or registry lookups.
        return typeof(IAuditable).Assembly
            .GetTypes()
            .Where(x => x is { IsAbstract: false, IsInterface: false } && x.IsAssignableTo(typeof(IAuditable)))
            .Select(x => (IAuditable)RuntimeHelpers.GetUninitializedObject(x))
            .ToList();
    }
}
