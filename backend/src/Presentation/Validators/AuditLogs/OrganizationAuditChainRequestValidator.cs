using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.AuditLogs;

namespace Presentation.Validators.AuditLogs;

public class OrganizationExportAuditLogRequestValidator : AbstractValidator<OrganizationExportAuditLogRequestDto>
{
    public OrganizationExportAuditLogRequestValidator(IOrganizationService organizationService, TimeProvider timeProvider)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("From Date cannot be in the future.");

        RuleFor(x => new { x.FromDate, x.ToDate })
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("From Date must be before or equal to To Date.");

        RuleFor(x => x.EntityType)
            .IsEnumName(typeof(AuditableEntityType), false)
            .When(x => !string.IsNullOrWhiteSpace(x.EntityType));

        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.EntityType) == !x.EntityId.HasValue)
            .WithMessage("Entity Type and Entity Id must be given together.");
    }
}

public class OrganizationVerifyAuditChainRequestValidator : AbstractValidator<OrganizationVerifyAuditChainRequestDto>
{
    public OrganizationVerifyAuditChainRequestValidator(IOrganizationService organizationService, TimeProvider timeProvider)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("From Date cannot be in the future.");

        RuleFor(x => new { x.FromDate, x.ToDate })
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("From Date must be before or equal to To Date.");
    }
}
