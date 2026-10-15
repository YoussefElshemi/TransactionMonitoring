using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Resources;
using FluentValidation.Results;
using Presentation.Models.Common;

namespace Presentation.Validators.Common;

public abstract class PagedRequestValidator<TPagedRequestDto> : AbstractValidator<TPagedRequestDto>
    where TPagedRequestDto : PagedRequestDto
{
    private const int MaxPageSize = 100;

    protected PagedRequestValidator(TimeProvider timeProvider)
    {
        // ReSharper disable VirtualMemberCallInConstructor
        Initialize();

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("From Date cannot be in the future.");

        RuleFor(x => new { x.FromDate, x.ToDate })
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("From Date must be before or equal to To Date.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(MaxPageSize);

        RuleFor(x => x)
            .CustomAsync(ValidatePageNumberAsync);
    }

    protected virtual void Initialize()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
    }

    protected abstract Task<TotalCount> GetCountAsync(TPagedRequestDto request, CancellationToken stoppingToken);

    private async Task ValidatePageNumberAsync(TPagedRequestDto request, ValidationContext<TPagedRequestDto> context, CancellationToken stoppingToken)
    {
        var totalCount = await GetCountAsync(request, stoppingToken);
        var maxPageNumber = request.GetPageCount(totalCount);

        if (request.PageNumber > maxPageNumber)
        {
            context.MessageFormatter.AppendPropertyName(nameof(request.PageNumber));
            context.MessageFormatter.AppendArgument("ComparisonValue", maxPageNumber);

            var errorMessage = context.MessageFormatter.BuildMessage(
                new LanguageManager().GetString("LessThanOrEqualValidator"));

            context.AddFailure(new ValidationFailure(nameof(request.PageNumber), errorMessage)
            {
                ErrorCode = "LessThanOrEqualValidator"
            });
        }
    }
}