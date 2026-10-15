using System.Net;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.ApiClients;

namespace Presentation.Validators.ApiClients;

public class OrganizationApiClientRequestValidator : AbstractValidator<OrganizationApiClientRequestDto>
{
    public OrganizationApiClientRequestValidator(IOrganizationService organizationService, IApiClientService apiClientService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");
    }
}

public class OrganizationRecoverApiClientRequestValidator : AbstractValidator<OrganizationRecoverApiClientRequestDto>
{
    public OrganizationRecoverApiClientRequestValidator(IOrganizationService organizationService, IApiClientService apiClientService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");
    }
}

public class OrganizationUpdateApiClientRequestValidator : AbstractValidator<OrganizationUpdateApiClientRequestDto>
{
    public OrganizationUpdateApiClientRequestValidator(
        IOrganizationService organizationService,
        IApiClientService apiClientService,
        IApiVersionService apiVersionService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");

        RuleFor(x => x.Body.FriendlyName)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.Body.FriendlyName != null);

        RuleFor(x => x.Body.Scopes)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("At least one scope is required.")
            .Must(x => x!.All(scope => ApiScopeHelper.TryParse(scope, out _)))
            .WithMessage("One or more scopes are not recognised.")
            .When(x => x.Body.Scopes != null);

        RuleFor(x => x.Body.ApiVersion)
            .Must(x => ApiVersion.TryParse(x, out var apiVersion) && apiVersionService.Find(apiVersion) != null)
            .WithMessage("'{PropertyValue}' is not an API version this installation serves. The served versions are listed at /api-versions.")
            .When(x => x.Body.ApiVersion != null);
    }
}

public class OrganizationApiClientSecretRequestValidator : AbstractValidator<OrganizationApiClientSecretRequestDto>
{
    public OrganizationApiClientSecretRequestValidator(IOrganizationService organizationService, IApiClientService apiClientService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");

        RuleFor(x => x.ApiClientSecretId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.SecretExistsAsync(
                new ApiClientId(x.ApiClientId), new ApiClientSecretId(x.ApiClientSecretId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client Secret not found");
    }
}
