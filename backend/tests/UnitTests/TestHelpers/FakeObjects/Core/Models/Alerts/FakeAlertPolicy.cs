using AutoFixture;
using Core.Enums;
using Core.Models.Alerts;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;

public static class FakeAlertPolicy
{
    public static AlertPolicy CreateValid(IFixture fixture)
    {
        return new AlertPolicy
        {
            SubjectRole = PartyRole.Remitter,
            DeduplicationWindow = FakeDeduplicationWindow.CreateValid(fixture),
            CaseGroupingEnabled = new IsEnabled(true),
            DeduplicationEnabled = new IsEnabled(true),
            CaseGroupingScope = CaseGroupingScope.Party,
            CaseGroupingSeverityPolicy = CaseGroupingSeverityPolicy.Any,
            MaxAlertsPerCase = null,
            CaseGroupingPeriod = FakeCaseGroupingPeriod.CreateValid(fixture)
        };
    }
}
