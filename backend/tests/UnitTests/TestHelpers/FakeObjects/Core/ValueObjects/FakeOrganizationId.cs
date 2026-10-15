using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeOrganizationId
{
    public static OrganizationId CreateValid(IFixture fixture)
    {
        return new OrganizationId(fixture.Create<Guid>());
    }
}