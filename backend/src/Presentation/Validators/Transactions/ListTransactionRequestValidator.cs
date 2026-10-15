using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Transactions;
using Presentation.Models.Transactions;
using Presentation.Validators.Common;

namespace Presentation.Validators.Transactions;

public class ListTransactionRequestValidator(
    ITransactionService transactionService,
    TimeProvider timeProvider) : PagedRequestValidator<ListTransactionRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListTransactionRequestDto request, CancellationToken stoppingToken)
    {
        return transactionService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
