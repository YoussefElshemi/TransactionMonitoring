using AutoFixture;
using Core.Models.SuspiciousActivityReports;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;

public static class FakeNarrativeSource
{
    public static NarrativeSource CreateValid(IFixture fixture)
    {
        return new NarrativeSource
        {
            Case = FakeCase.CreateValid(fixture),
            Transaction = null,
            Notes = [],
            RuleEvaluations = [],
            Parties = []
        };
    }
}
