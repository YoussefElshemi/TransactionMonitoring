using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.SubOrganizations;
using Presentation.Models.SubOrganizations;
using Presentation.Validators.Common;

namespace Presentation.Validators.SubOrganizations;

public class CreateSubOrganizationRequestValidator : AbstractValidator<CreateSubOrganizationRequestDto>
{
    public CreateSubOrganizationRequestValidator(IOrganizationService organizationService, ISubOrganizationService subOrganizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(SubOrganizationName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await subOrganizationService.FindByNameAsync(
                    new OrganizationId(x.OrganizationId), new SubOrganizationName(x.Body.Name), ct);

                return existing is null;
            })
            .WithMessage("Sub-organization name is already declared for this organization");

        RuleFor(x => x.Body.Description)
            .MaximumLength(SubOrganizationDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.ExternalReference)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.ExternalReference));
    }
}

public class SubOrganizationRequestValidator : AbstractValidator<SubOrganizationRequestDto>
{
    public SubOrganizationRequestValidator(IOrganizationService organizationService, ISubOrganizationService subOrganizationService)
    {
        this.AddSubOrganizationRules(organizationService, subOrganizationService, new IncludeDeleted(false));
    }
}

public class DeleteSubOrganizationRequestValidator : AbstractValidator<DeleteSubOrganizationRequestDto>
{
    public DeleteSubOrganizationRequestValidator(IOrganizationService organizationService, ISubOrganizationService subOrganizationService)
    {
        this.AddSubOrganizationRules(organizationService, subOrganizationService, new IncludeDeleted(false));
    }
}

public class RecoverSubOrganizationRequestValidator : AbstractValidator<RecoverSubOrganizationRequestDto>
{
    public RecoverSubOrganizationRequestValidator(IOrganizationService organizationService, ISubOrganizationService subOrganizationService)
    {
        this.AddSubOrganizationRules(organizationService, subOrganizationService, new IncludeDeleted(true));
    }
}

public class UpdateSubOrganizationRequestValidator : AbstractValidator<UpdateSubOrganizationRequestDto>
{
    public UpdateSubOrganizationRequestValidator(IOrganizationService organizationService, ISubOrganizationService subOrganizationService)
    {
        this.AddSubOrganizationRules(organizationService, subOrganizationService, new IncludeDeleted(false));

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(SubOrganizationName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await subOrganizationService.FindByNameAsync(
                    new OrganizationId(x.OrganizationId), new SubOrganizationName(x.Body.Name!), ct);

                return existing is null || existing.SubOrganizationId == x.SubOrganizationId;
            })
            .WithMessage("Sub-organization name is already declared for this organization")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Name));

        RuleFor(x => x.Body.Description)
            .MaximumLength(SubOrganizationDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));
    }
}

public class ListSubOrganizationRequestValidator(ISubOrganizationService subOrganizationService, TimeProvider timeProvider)
    : PagedRequestValidator<OrganizationListSubOrganizationRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Name)
            .MaximumLength(SubOrganizationName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
    }

    protected override Task<TotalCount> GetCountAsync(OrganizationListSubOrganizationRequestDto request, CancellationToken stoppingToken)
    {
        return subOrganizationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}

internal static class SubOrganizationRequestValidatorExtensions
{
    public static void AddSubOrganizationRules<T>(
        this AbstractValidator<T> validator,
        IOrganizationService organizationService,
        ISubOrganizationService subOrganizationService,
        IncludeDeleted includeDeleted)
        where T : SubOrganizationRequestDto
    {
        validator.RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        validator.RuleFor(x => x.SubOrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.SubOrganizationId), includeDeleted, ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found");
    }
}
