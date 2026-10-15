using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.SuspiciousActivityReports;

public record SuspiciousActivityReportRequestDto
{
    [FromRoute]
    public required Guid SuspiciousActivityReportId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record ListSuspiciousActivityReportRequestDto : PagedRequestDto
{
    public string? Status { get; init; }
    public Guid? CaseId { get; init; }
    public bool? IsOverdue { get; init; }
}

// Separate type so it gets its own validator.
public record GetCaseSuspiciousActivityReportRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record OpenSuspiciousActivityReportRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromBody]
    public OpenSuspiciousActivityReportRequestBodyDto? Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record OpenSuspiciousActivityReportRequestBodyDto
{
    public string? Narrative { get; init; }
}

// Separate type so it gets its own validator.
public record UpdateSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto
{
    [FromBody]
    public required UpdateSuspiciousActivityReportRequestBodyDto Body { get; init; }
}

public record UpdateSuspiciousActivityReportRequestBodyDto
{
    public required string Narrative { get; init; }
}

public record RecomposeSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto;

public record SubmitSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto
{
    [FromBody]
    public SubmitSuspiciousActivityReportRequestBodyDto? Body { get; init; }
}

public record SubmitSuspiciousActivityReportRequestBodyDto
{
    public string? Reason { get; init; }
}

public record FileSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto
{
    [FromBody]
    public required FileSuspiciousActivityReportRequestBodyDto Body { get; init; }
}

public record FileSuspiciousActivityReportRequestBodyDto
{
    public required string FilingReference { get; init; }
}

public record AcknowledgeSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto
{
    [FromBody]
    public AcknowledgeSuspiciousActivityReportRequestBodyDto? Body { get; init; }
}

public record AcknowledgeSuspiciousActivityReportRequestBodyDto
{
    public string? AcknowledgementReference { get; init; }
}

public record DeclineSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto
{
    [FromBody]
    public required DeclineSuspiciousActivityReportRequestBodyDto Body { get; init; }
}

public record DeclineSuspiciousActivityReportRequestBodyDto
{
    public required string Reason { get; init; }
}

public record ExportSuspiciousActivityReportRequestDto : SuspiciousActivityReportRequestDto;
