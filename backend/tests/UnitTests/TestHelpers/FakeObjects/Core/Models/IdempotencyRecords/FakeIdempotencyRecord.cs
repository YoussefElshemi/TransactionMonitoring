using AutoFixture;
using Core.Enums;
using Core.Models.IdempotencyRecords;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.IdempotencyRecords;

public static class FakeIdempotencyRecord
{
    public static IdempotencyRecord CreateValid(IFixture fixture)
    {
        return new IdempotencyRecord
        {
            Mode = Mode.Live,
            IdempotencyKey = new IdempotencyKey(fixture.Create<Guid>()),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Resource = new Resource("POST /rules/evaluate"),
            StatusCode = new StatusCode(200),
            ResponseBody = new ResponseBody("""{"result":true}"""),
            ExpiresAt = new ExpiresAt(fixture.Create<DateTimeOffset>()),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
