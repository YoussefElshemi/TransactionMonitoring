using Core.Constants;
using Core.Enums;
using Core.Localization;
using Core.Models.Tax;
using Core.ValueObjects;

namespace Core.Helpers;

public static class TaxDeterminer
{
    public static TaxDetermination Determine(
        CountryCode supplierCountry,
        CountryCode customerCountry,
        TaxIdentifier? customerTaxIdentifier,
        VerifiedAt? taxIdentifierVerifiedAt,
        Translator translator)
    {
        var supplier = TaxJurisdictions.Find(supplierCountry);
        var customer = TaxJurisdictions.Find(customerCountry);

        if (supplier is null)
        {
            return OutOfScope(translator.Text(TranslationKeys.TaxLineNoJurisdiction));
        }

        if (customerCountry == supplierCountry)
        {
            return new TaxDetermination
            {
                TaxTreatment = TaxTreatment.Standard,
                TaxRate = supplier.StandardRate,
                Description = new InvoiceLineDescription(translator.Text(
                    TranslationKeys.TaxLineDomestic, supplier.Name, translator.Number(supplier.StandardRate, "0.##")))
            };
        }

        if (customer is not null && customer.IsEuMember)
        {
            if (customerTaxIdentifier.HasValue && taxIdentifierVerifiedAt.HasValue)
            {
                return new TaxDetermination
                {
                    TaxTreatment = TaxTreatment.ReverseCharge,
                    TaxRate = new TaxRate(0m),
                    Description = new InvoiceLineDescription(translator.Text(TranslationKeys.TaxLineReverseCharge))
                };
            }

            if (supplier.IsEuMember)
            {
                return new TaxDetermination
                {
                    TaxTreatment = TaxTreatment.Standard,
                    TaxRate = customer.StandardRate,
                    Description = new InvoiceLineDescription(translator.Text(
                        TranslationKeys.TaxLineOneStopShop, customer.Name, translator.Number(customer.StandardRate, "0.##")))
                };
            }
        }

        return OutOfScope(translator.Text(TranslationKeys.TaxLineOutOfScope, supplier.Name));
    }

    private static TaxDetermination OutOfScope(string description)
    {
        return new TaxDetermination
        {
            TaxTreatment = TaxTreatment.OutOfScope,
            TaxRate = new TaxRate(0m),
            Description = new InvoiceLineDescription(description)
        };
    }
}
