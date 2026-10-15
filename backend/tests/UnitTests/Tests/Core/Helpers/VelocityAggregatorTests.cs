using Core.Enums;
using Core.Helpers;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class VelocityAggregatorTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Aggregate_GivenASum_TotalsTheNumbers()
    {
        // Arrange
        var query = Query();
        var aggregate = VelocityAggregator.Reduce(Observations(100m, 250m, 75m), query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.Sum, query, aggregate);

        // Assert
        result.Should().Be(425m);
    }

    [Fact]
    public void Aggregate_GivenACount_CountsEveryObservationIncludingTheOnesWithNoNumber()
    {
        // Arrange
        var query = Query();
        var aggregate = VelocityAggregator.Reduce(Observations(100m, null, 75m), query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.Count, query, aggregate);

        // Assert
        result.Should().Be(3m);
    }

    [Fact]
    public void Aggregate_GivenADistinctCount_CountsEachValueOnce()
    {
        // Arrange
        var query = Query();
        var aggregate = VelocityAggregator.Reduce([Text("ACC-1"), Text("ACC-2"), Text("ACC-1")], query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.DistinctCount, query, aggregate);

        // Assert
        result.Should().Be(2m);
    }

    [Fact]
    public void Aggregate_GivenAnEmptyWindow_CountsZero()
    {
        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.Count, Query(), VelocityAggregate.Empty);

        // Assert
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData(VelocityFunction.Sum)]
    [InlineData(VelocityFunction.Min)]
    [InlineData(VelocityFunction.Max)]
    [InlineData(VelocityFunction.Average)]
    public void Aggregate_GivenAnEmptyWindow_AnswersNullForTheNumericAggregates(VelocityFunction function)
    {
        // Act
        var result = VelocityAggregator.Aggregate(function, Query(), VelocityAggregate.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Aggregate_GivenObservationsWithNoNumber_AveragesOnlyTheOnesThatHaveOne()
    {
        // Arrange
        var query = Query();
        var aggregate = VelocityAggregator.Reduce(Observations(100m, null, 200m), query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.Average, query, aggregate);

        // Assert
        result.Should().Be(150m);
    }

    [Fact]
    public void Aggregate_GivenAStructuringCount_CountsWhatSitsJustUnderTheThreshold()
    {
        // Arrange
        var query = Query(band: true);
        var aggregate = VelocityAggregator.Reduce(Observations(9500m, 9200m, 8100m, 4000m, 12000m), query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.StructuringCount, query, aggregate);

        // Assert
        result.Should().Be(3m);
    }

    [Fact]
    public void Aggregate_GivenAPaymentExactlyAtTheThreshold_DoesNotCountIt()
    {
        // Arrange
        var query = Query(band: true);
        var aggregate = VelocityAggregator.Reduce(Observations(10000m), query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.StructuringCount, query, aggregate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void Aggregate_GivenAStructuringCountWithNoBand_AnswersNull()
    {
        // Arrange
        var query = Query();
        var aggregate = VelocityAggregator.Reduce(Observations(9500m), query);

        // Act
        var result = VelocityAggregator.Aggregate(VelocityFunction.StructuringCount, query, aggregate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Reduce_GivenAWindowExcludingTheCurrentPayment_LeavesItOut()
    {
        // Arrange
        var observations = new List<VelocityObservationFact> { Number(6000m, false), Number(5000m, true) };

        // Act
        var including = VelocityAggregator.Reduce(observations, Query());
        var excluding = VelocityAggregator.Reduce(observations, Query(includeCurrent: false));

        // Assert
        ((int)including.Count).Should().Be(2);
        ((int)excluding.Count).Should().Be(1);
    }

    [Fact]
    public void Reduce_GivenAnObservationOutsideTheWindow_LeavesItOut()
    {
        // Arrange
        var observations = new List<VelocityObservationFact> { Number(6000m, false, Now.AddDays(-3)), Number(5000m, false) };

        // Act
        var aggregate = VelocityAggregator.Reduce(observations, Query());

        // Assert
        ((int)aggregate.Count).Should().Be(1);
    }

    [Fact]
    public void Band_GivenAThresholdAndABand_OpensTheBandBelowIt()
    {
        // Act
        var (floor, ceiling) = VelocityAggregator.Band(10000m, 0.2m);

        // Assert
        floor.Should().Be(8000m);
        ceiling.Should().Be(10000m);
    }

    private static VelocityWindowQuery Query(bool band = false, bool includeCurrent = true)
    {
        var (floor, ceiling) = VelocityAggregator.Band(10000m, 0.2m);

        return new VelocityWindowQuery
        {
            Role = PartyRole.Remitter,
            Metric = new VelocityMetricName("amount"),
            From = new ObservedAt(Now.AddDays(-1)),
            To = new ObservedAt(Now),
            IncludeCurrent = new IncludeCurrent(includeCurrent),
            ExcludeTransactionId = null,
            BandFloor = band ? new MetricNumber(floor) : null,
            BandCeiling = band ? new MetricNumber(ceiling) : null
        };
    }

    private static List<VelocityObservationFact> Observations(params decimal?[] numbers)
    {
        return numbers.Select(x => Fact(x?.ToString() ?? "none", x, false, Now)).ToList();
    }

    private static VelocityObservationFact Number(decimal number, bool isCurrent, DateTimeOffset? observedAt = null)
    {
        return Fact(number.ToString(), number, isCurrent, observedAt ?? Now);
    }

    private static VelocityObservationFact Text(string value)
    {
        return Fact(value, null, false, Now);
    }

    private static VelocityObservationFact Fact(string text, decimal? number, bool isCurrent, DateTimeOffset observedAt)
    {
        return new VelocityObservationFact
        {
            PartyId = new PartyId(Guid.NewGuid()),
            Role = PartyRole.Remitter,
            Metric = new VelocityMetricName("amount"),
            ObservedAt = new ObservedAt(observedAt),
            Text = new MetricText(text),
            Number = number.HasValue ? new MetricNumber(number.Value) : null,
            IsCurrent = new IsCurrent(isCurrent)
        };
    }
}
