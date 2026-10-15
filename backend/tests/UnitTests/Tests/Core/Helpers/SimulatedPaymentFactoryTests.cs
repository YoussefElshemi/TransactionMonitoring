using Core.Enums;
using Core.Helpers;
using Core.Models.PartyMappings;
using Core.Models.Simulations;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.PartyMappings;
using UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;

namespace UnitTests.Tests.Core.Helpers;

public class SimulatedPaymentFactoryTests : BaseTestClass
{
    [Fact]
    public void Create_Always_EmitsTheRequestedNumberOfPayments()
    {
        // Act
        var payments = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Baseline, 25));

        // Assert
        payments.Should().HaveCount(25);
    }

    [Fact]
    public void Create_GivenTheSameSeed_EmitsTheSamePayments()
    {
        // Act
        var first = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Mixed, 30));
        var second = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Mixed, 30));

        // Assert
        first.ConvertAll(x => x.Payload.ToJsonString())
            .Should().Equal(second.ConvertAll(x => x.Payload.ToJsonString()));
    }

    [Fact]
    public void Create_GivenADifferentSeed_EmitsDifferentPayments()
    {
        // Act
        var first = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Baseline, 30));
        var second = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Baseline, 30, seed: 99));

        // Assert
        first.ConvertAll(x => x.Payload.ToJsonString())
            .Should().NotEqual(second.ConvertAll(x => x.Payload.ToJsonString()));
    }

    [Fact]
    public void Create_Always_GivesEveryPaymentItsOwnExternalReference()
    {
        // Act
        var payments = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Baseline, 40));

        // Assert
        payments.Select(x => x.ExternalReference.ToString()).Distinct().Should().HaveCount(40);
    }

    [Fact]
    public void Create_GivenStructuring_EmitsRepeatedAmountsJustUnderTheRoundFigure()
    {
        // Act
        var payments = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Structuring, 20));

        // Assert
        var amounts = payments.ConvertAll(x => x.Payload["amount"]!.GetValue<decimal>());

        amounts.Should().OnlyContain(x => x >= 9000m && x < 10_000m);

        payments.Select(x => x.Payload["remitter"]!["accountId"]!.GetValue<string>())
            .Distinct().Should().HaveCount(1);
    }

    [Fact]
    public void Create_GivenAVelocitySpike_EmitsRepeatedPaymentsFromOneParty()
    {
        // Act
        var payments = SimulatedPaymentFactory.Create(Plan(SimulationScenario.VelocitySpike, 20));

        // Assert
        payments.Select(x => x.Payload["remitter"]!["accountId"]!.GetValue<string>())
            .Distinct().Should().HaveCount(1);
    }

    [Fact]
    public void Create_GivenHighValue_EmitsAmountsFarAboveTheOrdinary()
    {
        // Act
        var payments = SimulatedPaymentFactory.Create(Plan(SimulationScenario.HighValue, 10));

        // Assert
        payments.ConvertAll(x => x.Payload["amount"]!.GetValue<decimal>())
            .Should().OnlyContain(x => x >= 75_000m);
    }

    [Fact]
    public void Create_GivenMixed_EmitsMoreThanOneShape()
    {
        // Act
        var payments = SimulatedPaymentFactory.Create(Plan(SimulationScenario.Mixed, 60));

        // Assert
        payments.Select(x => x.Payload["remitter"]!["accountId"]!.GetValue<string>())
            .Distinct().Count().Should().BeGreaterThan(1);
    }

    [Fact]
    public void Create_GivenASchema_EmitsOnlyTheDeclaredFields()
    {
        // Arrange
        var plan = Plan(SimulationScenario.Baseline, 5) with
        {
            TransactionSchema = Schema()
        };

        // Act
        var payments = SimulatedPaymentFactory.Create(plan);

        // Assert
        foreach (var payment in payments)
        {
            payment.Payload.Select(x => x.Key).Should().BeEquivalentTo("amount", "currency", "senderAccount");
            payment.Payload["currency"]!.GetValue<string>().Should().BeOneOf("GBP", "EUR");
        }
    }

    [Fact]
    public void Create_GivenAPartyMapping_WritesTheIdentifierWhereTheMappingReadsIt()
    {
        // Arrange
        var plan = Plan(SimulationScenario.Baseline, 5) with
        {
            PartyMapping = Mapping()
        };

        // Act
        var payments = SimulatedPaymentFactory.Create(plan);

        // Assert
        foreach (var payment in payments)
        {
            payment.Payload["parties"]!["from"]!["id"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        }
    }

    private SimulationPlan Plan(SimulationScenario scenario, int count, int seed = 42)
    {
        return new SimulationPlan
        {
            SimulationId = new SimulationId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            Scenario = scenario,
            PaymentCount = new PaymentCount(count),
            Seed = new SimulationSeed(seed),
            TransactionSchema = null,
            PartyMapping = null,
            GeneratedAt = new EvaluatedAt(MockTimeProvider.GetUtcNow())
        };
    }

    private TransactionSchema Schema()
    {
        return FakeTransactionSchema.CreateValid(Fixture) with
        {
            Fields =
            [
                new SchemaField
                {
                    Name = new SchemaFieldName("amount"),
                    Type = SchemaFieldType.Number,
                    IsRequired = new IsRequired(true),
                    AllowedValues = null
                },
                new SchemaField
                {
                    Name = new SchemaFieldName("currency"),
                    Type = SchemaFieldType.String,
                    IsRequired = new IsRequired(true),
                    AllowedValues = ["GBP", "EUR"]
                },
                new SchemaField
                {
                    Name = new SchemaFieldName("senderAccount"),
                    Type = SchemaFieldType.String,
                    IsRequired = new IsRequired(true),
                    AllowedValues = null
                }
            ]
        };
    }

    private PartyMapping Mapping()
    {
        return FakePartyMapping.CreateValid(Fixture) with
        {
            Mappings =
            [
                new PartyRoleMapping
                {
                    Role = PartyRole.Remitter,
                    Path = new PartyPath("parties.from"),
                    IdentifierPath = new PartyPath("id"),
                    DisplayNamePath = new PartyPath("name")
                }
            ]
        };
    }
}
