using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.TransactionSchemas;

public record ListTransactionSchemaVersionRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}

public record ListMyTransactionSchemaVersionRequestDto : PagedRequestDto;
