using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Transactions;

namespace Presentation.Validators.Transactions;

public class RevealTransactionRequestValidator : AbstractValidator<RevealTransactionRequestDto>
{
    public RevealTransactionRequestValidator(ITransactionService transactionService)
    {
        RuleFor(x => x.TransactionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await transactionService.ExistsAsync(new OrganizationId(x.OrganizationId), new TransactionId(x.TransactionId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Transaction not found");

        RuleFor(x => x.Body.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(RevealReason.MinLength)
            .MaximumLength(RevealReason.MaxLength);
    }
}
