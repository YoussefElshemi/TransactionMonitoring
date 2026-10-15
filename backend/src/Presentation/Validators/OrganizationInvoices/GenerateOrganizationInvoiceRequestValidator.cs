using System.Net;
using Core.Interfaces.Services;
using Core.Models.Invoices;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.OrganizationInvoices;

namespace Presentation.Validators.OrganizationInvoices;

public class GenerateOrganizationInvoiceRequestValidator : AbstractValidator<GenerateOrganizationInvoiceRequestDto>
{
    private readonly IInvoiceService _invoiceService;
    private readonly TimeProvider _timeProvider;

    public GenerateOrganizationInvoiceRequestValidator(
        IOrganizationService organizationService,
        IInvoiceService invoiceService,
        TimeProvider timeProvider)
    {
        _invoiceService = invoiceService;
        _timeProvider = timeProvider;
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Month)
            .Must(BeAValidMonth)
            .WithMessage($"{nameof(Month)} must be between 1 and 12");

        RuleFor(x => x.Body.Year)
            .Must(BeAValidYear)
            .WithMessage($"{nameof(Year)} must be between 1 and this year");

        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(BeInThePast)
            .WithMessage("Time period provided must be in the past, and the month has ended")
            .MustAsync(NotAlreadyExist)
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage($"{nameof(Invoice)} already exists for this time period")
            .When(x => BeAValidMonth(x.Body.Month) && BeAValidYear(x.Body.Year));
    }

    private static bool BeAValidMonth(int month)
    {
        return month is >= 1 and <= 12;
    }

    private bool BeAValidYear(int year)
    {
        return year >= 1 && year <= _timeProvider.GetUtcNow().Year;
    }

    private bool BeInThePast(GenerateOrganizationInvoiceRequestDto generateInvoiceRequestDto)
    {
        var daysInMonth = DateTime.DaysInMonth(generateInvoiceRequestDto.Body.Year, generateInvoiceRequestDto.Body.Month);
        var toDate = new DateOnly(generateInvoiceRequestDto.Body.Year, generateInvoiceRequestDto.Body.Month, daysInMonth);

        return toDate < DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
    }

    private async Task<bool> NotAlreadyExist(GenerateOrganizationInvoiceRequestDto generateInvoiceRequestDto, CancellationToken stoppingToken)
    {
        var exists = await _invoiceService.ExistsByDateAsync(
            new OrganizationId(generateInvoiceRequestDto.OrganizationId),
            new Month(generateInvoiceRequestDto.Body.Month),
            new Year(generateInvoiceRequestDto.Body.Year),
            new IncludeDeleted(false),
            stoppingToken);

        return !exists;
    }
}