using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.BackTests;
using Presentation.Models.BackTests;
using Presentation.Validators.Common;

namespace Presentation.Validators.BackTests;

public class ListBackTestResultRequestValidator(
    IBackTestService backTestService,
    IBackTestResultService backTestResultService,
    TimeProvider timeProvider) : PagedRequestValidator<ListBackTestResultRequestDto>(timeProvider)
{
    protected override void Initialize()
    {
        base.Initialize();
        RuleFor(x => x.BackTestId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await backTestService.ExistsAsync(new OrganizationId(x.OrganizationId), new BackTestId(x.BackTestId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("BackTest not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListBackTestResultRequestDto request, CancellationToken stoppingToken)
    {
        return backTestResultService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
