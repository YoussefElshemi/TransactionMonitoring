using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.ApiClients;

namespace Presentation.Validators.ApiClients;

public class CreateApiClientSecretRequestValidator : AbstractValidator<CreateApiClientSecretRequestDto>
{
    public CreateApiClientSecretRequestValidator(IApiClientService apiClientService, TimeProvider timeProvider)
    {
        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");

        RuleFor(x => x.Body.ExpiresAt)
            .GreaterThan(x => timeProvider.GetUtcNow())
            .WithMessage("Expires At must be in the future.")
            .When(x => x.Body.ExpiresAt.HasValue);
    }
}
