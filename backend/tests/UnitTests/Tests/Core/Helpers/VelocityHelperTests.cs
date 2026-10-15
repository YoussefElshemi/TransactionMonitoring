using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class VelocityHelperTests : BaseTestClass
{
    [Fact]
    public void ValidateRuleBody_GivenAMetricTheProfileDeclares_PassesValidation()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, "amount");

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), null);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenAMetricTheProfileDoesNotDeclare_IsRejected()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, "fee");

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), null);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("not a metric");
    }

    [Fact]
    public void ValidateRuleBody_GivenNoProfileAtAll_IsRejected()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, "amount");

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, null, null);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("no velocity profile");
    }

    [Fact]
    public void ValidateRuleBody_GivenAStructuringCountWithNoThreshold_IsRejected()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.StructuringCount, "amount");

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), null);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("must name a Threshold and a Band");
    }

    [Fact]
    public void ValidateRuleBody_GivenAStructuringCountNamingAnUndeclaredThreshold_IsRejected()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.StructuringCount, "amount", threshold: "cash", band: 0.2m);

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), null);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("not a threshold");
    }

    [Fact]
    public void ValidateRuleBody_GivenAStructuringCountNamingADeclaredThreshold_PassesValidation()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.StructuringCount, "amount", threshold: "reporting", band: 0.2m);

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), null);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenAThresholdOnSomethingOtherThanAStructuringCount_IsRejected()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, "amount", threshold: "reporting", band: 0.2m);

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), null);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("only a 'StructuringCount' reads");
    }

    [Fact]
    public void ValidateRuleBody_GivenASumOverAMetricTheSchemaSaysIsAString_IsRejected()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, "country");

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), Schema());

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("can never total anything");
    }

    [Fact]
    public void ValidateRuleBody_GivenADistinctCountOverAMetricTheSchemaSaysIsAString_PassesValidation()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.DistinctCount, "country");

        // Act
        var violations = VelocityHelper.ValidateRuleBody(rule, Profile(), Schema());

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void CollectNames_GivenARuleNamingAMetricAndAThreshold_ReportsBoth()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.StructuringCount, "amount", threshold: "reporting", band: 0.2m);

        // Act
        var (metrics, thresholds) = VelocityHelper.CollectNames(rule);

        // Assert
        metrics.Should().ContainSingle(x => x == "amount");
        thresholds.Should().ContainSingle(x => x == "reporting");
    }

    private static RuleBodyModel VelocityRule(VelocityFunction function, string metric, string? threshold = null, decimal? band = null)
    {
        return new RuleBodyModel
        {
            Condition = RuleCondition.GreaterThan,
            Velocity = new VelocityConditionModel
            {
                Role = PartyRole.Remitter,
                Metric = metric,
                Function = function,
                Window = new VelocityWindowModel { Kind = VelocityWindowKind.Rolling, Unit = VelocityWindowUnit.Day, Length = 1 },
                IncludeCurrent = true,
                Threshold = threshold,
                Band = band
            },
            Value = 3
        };
    }

    private static VelocityProfile Profile()
    {
        var now = DateTimeOffset.UnixEpoch;

        return new VelocityProfile
        {
            Mode = Mode.Live,
            VelocityProfileId = new VelocityProfileId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            Metrics =
            [
                new VelocityMetric { Name = new VelocityMetricName("amount"), Path = new VelocityMetricPath("amount") },
                new VelocityMetric { Name = new VelocityMetricName("country"), Path = new VelocityMetricPath("country") }
            ],
            Thresholds =
            [
                new VelocityThreshold { Name = new VelocityThresholdName("reporting"), Value = new VelocityThresholdValue(10000m) }
            ],
            CreatedByUserId = new ApplicationUserId(Guid.NewGuid()),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    private static TransactionSchema Schema()
    {
        var now = DateTimeOffset.UnixEpoch;

        return new TransactionSchema
        {
            Mode = Mode.Live,
            TransactionSchemaId = new TransactionSchemaId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            Fields =
            [
                Field("amount", SchemaFieldType.Number),
                Field("country", SchemaFieldType.String)
            ],
            CurrentVersionId = new TransactionSchemaVersionId(Guid.NewGuid()),
            CurrentVersionNumber = new VersionNumber(1),
            CreatedByUserId = new ApplicationUserId(Guid.NewGuid()),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    private static SchemaField Field(string name, SchemaFieldType type)
    {
        return new SchemaField
        {
            Name = new SchemaFieldName(name),
            Type = type,
            IsRequired = new IsRequired(false),
            AllowedValues = null,
            ElementType = null,
            Fields = null
        };
    }
}
