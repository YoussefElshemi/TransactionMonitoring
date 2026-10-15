using Core.Enums;
using FluentValidation;
using Presentation.Models.AuditLogs;

namespace Presentation.Validators.AuditLogs;

public class ExportAuditLogRequestValidator : AbstractValidator<ExportAuditLogRequestDto>
{
    public ExportAuditLogRequestValidator(TimeProvider timeProvider)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

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

public class VerifyAuditChainRequestValidator : AbstractValidator<VerifyAuditChainRequestDto>
{
    public VerifyAuditChainRequestValidator(TimeProvider timeProvider)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("From Date cannot be in the future.");

        RuleFor(x => new { x.FromDate, x.ToDate })
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("From Date must be before or equal to To Date.");
    }
}
