using AutoFixture;
using UnitTests.TestHelpers.FixtureCustomizations;

namespace BehaviouralTests.TestHelpers;

public static class AutoFixtureHelper
{
    public static Fixture Create()
    {
        var fixture = new Fixture();
        fixture.Customize(new ValidDateOnlyCustomization());
        fixture.Customize(new ValidDateTimeOffsetCustomization());
        return fixture;
    }
}