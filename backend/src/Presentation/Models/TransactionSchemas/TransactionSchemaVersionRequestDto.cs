using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.TransactionSchemas;

public record TransactionSchemaVersionRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required int VersionNumber { get; init; }
}

public record TransactionSchemaRequestDto : OrganizationRequestDto;

public record GetTransactionSchemaRequestDto : OrganizationRequestDto
{
    [FromQuery]
    public bool IncludeDeleted { get; init; }
}

public record RecoverTransactionSchemaRequestDto : OrganizationRequestDto;

public record TransactionSchemaHistoryRequestDto : OrganizationRequestDto;
