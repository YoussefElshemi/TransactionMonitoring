using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.TransactionSchemas;

public record SetTransactionSchemaRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required SetTransactionSchemaRequestBodyDto Body { get; init; }
}

public record SetTransactionSchemaRequestBodyDto
{
    public required List<SchemaFieldDto> Fields { get; init; }
}
