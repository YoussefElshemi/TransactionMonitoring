using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.ApiClients;

namespace Presentation.Validators.ApiClients;

public class RecoverApiClientRequestValidator : AbstractValidator<RecoverApiClientRequestDto>
{
    public RecoverApiClientRequestValidator(IApiClientService apiClientService)
    {
        RuleFor(x => x.ApiClientId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await apiClientService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApiClientId(x.ApiClientId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("API Client not found");
    }
}
