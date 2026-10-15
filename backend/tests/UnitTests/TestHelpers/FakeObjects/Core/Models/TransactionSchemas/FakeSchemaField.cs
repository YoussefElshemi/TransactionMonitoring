using AutoFixture;
using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;

public static class FakeSchemaField
{
    public static SchemaField CreateValid(IFixture fixture)
    {
        return new SchemaField
        {
            Name = new SchemaFieldName(fixture.Create<string>()),
            Type = SchemaFieldType.Number,
            IsRequired = new IsRequired(true),
            AllowedValues = null
        };
    }
}
