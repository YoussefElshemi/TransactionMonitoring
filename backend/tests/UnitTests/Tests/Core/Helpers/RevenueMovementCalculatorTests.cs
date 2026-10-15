using Core.Enums;
using Core.Helpers;
using Core.Models.Revenue;
using FluentAssertions;

namespace UnitTests.Tests.Core.Helpers;

public class RevenueMovementCalculatorTests
{
    private static readonly Guid Acme = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Borden = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Corvid = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly int January = BillingPeriod.Key(new(1), new(2026));
    private static readonly int February = BillingPeriod.Key(new(2), new(2026));
    private static readonly int March = BillingPeriod.Key(new(3), new(2026));

    [Fact]
    public void Calculate_GivenACustomerBillingForTheFirstTime_CountsItAsNew()
    {
        // Arrange
        var revenue = Revenue((February, (Acme, 100m)));

        // Act
        var months = Calculate(revenue, February, February, [(Acme, February)]);

        // Assert
        months.Should().ContainSingle();
        ((decimal)months[0].NewRevenue).Should().Be(100m);
        ((decimal)months[0].ReactivationRevenue).Should().Be(0m);
        ((int)months[0].NewCustomers).Should().Be(1);
    }

    [Fact]
    public void Calculate_GivenACustomerBillingAgainAfterAGap_CountsItAsReactivationRatherThanNew()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)), (March, (Acme, 100m)));

        // Act
        var months = Calculate(revenue, March, March, [(Acme, January)]);

        // Assert
        ((decimal)months[0].NewRevenue).Should().Be(0m);
        ((decimal)months[0].ReactivationRevenue).Should().Be(100m);
        ((int)months[0].ReactivatedCustomers).Should().Be(1);
        ((int)months[0].NewCustomers).Should().Be(0);
    }

    [Fact]
    public void Calculate_GivenACustomerBillingMoreThanLastMonth_ReportsTheDifferenceAsExpansion()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)), (February, (Acme, 175m)));

        // Act
        var months = Calculate(revenue, February, February, [(Acme, January)]);

        // Assert
        ((decimal)months[0].ExpansionRevenue).Should().Be(75m);
        ((decimal)months[0].ContractionRevenue).Should().Be(0m);
    }

    [Fact]
    public void Calculate_GivenACustomerBillingLessThanLastMonth_ReportsTheDifferenceAsContraction()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)), (February, (Acme, 60m)));

        // Act
        var months = Calculate(revenue, February, February, [(Acme, January)]);

        // Assert
        ((decimal)months[0].ContractionRevenue).Should().Be(40m);
        ((decimal)months[0].ExpansionRevenue).Should().Be(0m);
        ((int)months[0].ChurnedCustomers).Should().Be(0);
    }

    [Fact]
    public void Calculate_GivenExpansionAndContractionThatCancelOut_ReportsBothRatherThanNothing()
    {
        // Arrange
        var revenue = Revenue(
            (January, (Acme, 100m)), (January, (Borden, 100m)),
            (February, (Acme, 150m)), (February, (Borden, 50m)));

        // Act
        var months = Calculate(revenue, February, February, [(Acme, January), (Borden, January)]);

        // Assert
        ((decimal)months[0].ExpansionRevenue).Should().Be(50m);
        ((decimal)months[0].ContractionRevenue).Should().Be(50m);
        ((decimal)months[0].ClosingRevenue).Should().Be(months[0].OpeningRevenue);
    }

    [Fact]
    public void Calculate_GivenACustomerThatStoppedBilling_ReportsWhatTheyWereWorthAsChurn()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)));

        // Act
        var months = Calculate(revenue, February, February, [(Acme, January)]);

        // Assert
        ((decimal)months[0].ChurnedRevenue).Should().Be(100m);
        ((int)months[0].ChurnedCustomers).Should().Be(1);
        ((decimal)months[0].ClosingRevenue).Should().Be(0m);
    }

    [Fact]
    public void Calculate_GivenAChurnedCustomerBeingChased_CountsItAsInvoluntary()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)));

        // Act
        var months = Calculate(
            revenue, February, February,
            [(Acme, January)],
            arrears: (Acme, March));

        // Assert
        ((int)months[0].InvoluntarilyChurnedCustomers).Should().Be(1);
        ((decimal)months[0].InvoluntaryChurnedRevenue).Should().Be(100m);
    }

    [Fact]
    public void Calculate_GivenArrearsLongBeforeTheDeparture_CountsTheChurnAsVoluntary()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)));
        var longBefore = BillingPeriod.Key(new(6), new(2025));

        // Act
        var months = Calculate(
            revenue, February, February,
            [(Acme, January)],
            arrears: (Acme, longBefore));

        // Assert
        ((int)months[0].ChurnedCustomers).Should().Be(1);
        ((int)months[0].InvoluntarilyChurnedCustomers).Should().Be(0);
    }

    [Fact]
    public void Calculate_GivenEveryKindOfMovementAtOnce_ReconcilesToTheClosingFigure()
    {
        // Arrange
        var revenue = Revenue(
            (January, (Acme, 100m)), (January, (Borden, 80m)),
            (February, (Acme, 130m)), (February, (Corvid, 45m)));

        // Act
        var months = Calculate(
            revenue, February, February,
            [(Acme, January), (Borden, January), (Corvid, February)]);

        // Assert
        var month = months[0];

        var reconciled = month.OpeningRevenue
                         + month.NewRevenue
                         + month.ReactivationRevenue
                         + month.ExpansionRevenue
                         - month.ContractionRevenue
                         - month.ChurnedRevenue;

        ((decimal)month.ClosingRevenue).Should().Be(reconciled);
        ((decimal)month.ClosingRevenue).Should().Be(175m);
    }

    [Fact]
    public void Calculate_GivenAMonthCreditedBelowZero_DoesNotReportTheCustomerAsChurned()
    {
        // Arrange
        var revenue = Revenue((January, (Acme, 100m)), (February, (Acme, -20m)));

        // Act
        var months = Calculate(revenue, February, February, [(Acme, January)]);

        // Assert
        ((int)months[0].ChurnedCustomers).Should().Be(0);
        ((decimal)months[0].ContractionRevenue).Should().Be(120m);
        ((int)months[0].ClosingCustomers).Should().Be(1);
    }

    [Fact]
    public void Calculate_GivenARunOfMonths_OpensEachOneWhereTheLastOneClosed()
    {
        // Arrange
        var revenue = Revenue(
            (January, (Acme, 100m)),
            (February, (Acme, 150m)),
            (March, (Acme, 120m)));

        // Act
        var months = Calculate(revenue, February, March, [(Acme, January)]);

        // Assert
        months.Should().HaveCount(2);
        ((decimal)months[1].OpeningRevenue).Should().Be(months[0].ClosingRevenue);
    }

    private static List<RecurringRevenueMonth> Calculate(
        Dictionary<int, IReadOnlyDictionary<Guid, decimal>> revenue,
        int firstMonthKey,
        int lastMonthKey,
        (Guid Organization, int MonthKey)[] firstBilled,
        (Guid Organization, int MonthKey)? arrears = null)
    {
        var escalated = arrears.HasValue
            ? new Dictionary<Guid, IReadOnlySet<int>>
            {
                [arrears.Value.Organization] = new HashSet<int> { arrears.Value.MonthKey }
            }
            : [];

        return RevenueMovementCalculator.Calculate(
            revenue,
            firstBilled.ToDictionary(x => x.Organization, x => x.MonthKey),
            escalated,
            Currency.Gbp,
            firstMonthKey,
            lastMonthKey);
    }

    private static Dictionary<int, IReadOnlyDictionary<Guid, decimal>> Revenue(
        params (int MonthKey, (Guid Organization, decimal Amount) Entry)[] entries)
    {
        var revenue = new Dictionary<int, Dictionary<Guid, decimal>>();

        foreach (var (monthKey, entry) in entries)
        {
            if (!revenue.TryGetValue(monthKey, out var month))
            {
                month = revenue[monthKey] = [];
            }

            month[entry.Organization] = entry.Amount;
        }

        return revenue.ToDictionary(x => x.Key, x => (IReadOnlyDictionary<Guid, decimal>)x.Value);
    }
}
