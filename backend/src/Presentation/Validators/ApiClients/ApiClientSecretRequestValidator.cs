using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.ApiClients;

namespace Presentation.Validators.ApiClients;

public class ApiClientSecretRequestValidator : AbstractValidator<ApiClientSecretRequestDto>
{
    public ApiClientSecretRequestValidator(IApiClientService apiClientService)
    {
        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");

        RuleFor(x => x.ApiClientSecretId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.SecretExistsAsync(new ApiClientId(x.ApiClientId), new ApiClientSecretId(x.ApiClientSecretId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client Secret not found");
    }
}
