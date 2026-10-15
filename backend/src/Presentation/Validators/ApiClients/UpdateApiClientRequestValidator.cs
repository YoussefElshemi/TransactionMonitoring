using System.Net;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.ApiClients;

namespace Presentation.Validators.ApiClients;

public class UpdateApiClientRequestValidator : AbstractValidator<UpdateApiClientRequestDto>
{
    public UpdateApiClientRequestValidator(IApiClientService apiClientService, IApiVersionService apiVersionService)
    {
        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(false), ct))
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
