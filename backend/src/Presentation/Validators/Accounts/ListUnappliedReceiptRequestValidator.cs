using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Accounts;
using Presentation.Models.Accounts;
using Presentation.Validators.Common;

namespace Presentation.Validators.Accounts;

public class ListUnappliedReceiptRequestValidator(
    IAccountService accountService,
    TimeProvider timeProvider) : PagedRequestValidator<ListUnappliedReceiptRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListUnappliedReceiptRequestDto request, CancellationToken stoppingToken)
    {
        return accountService.GetUnappliedCountAsync(request.ToModel(), stoppingToken);
    }
}
