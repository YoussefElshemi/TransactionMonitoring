using AutoFixture;
using Presentation.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

public static class FakeCreateBackTestRequestBodyDto
{
    public static CreateBackTestRequestBodyDto CreateValid(IFixture fixture)
    {
        return new CreateBackTestRequestBodyDto
        {
             SampleSize = FakeSampleSize.Valid
        };
    }
}
