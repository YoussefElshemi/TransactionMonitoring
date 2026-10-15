using Core.Enums;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Search;

namespace Presentation.Validators.Search;

public class SearchRequestValidator : AbstractValidator<SearchRequestDto>
{
    private const int MaxLimit = 25;

    public SearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .Must(SearchQuery.IsValid)
            .WithMessage($"A query is between {SearchQuery.MinimumLength} and {SearchQuery.MaximumLength} characters.");

        RuleForEach(x => x.Types)
            .IsEnumName(typeof(SearchResultType), false);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit);
    }
}
