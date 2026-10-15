import type { BillingEntity, CurrencyCode } from "@/types";
import { isSupportedJurisdiction } from "@/lib/tax";

export const LEGAL_NAME_MAX_LENGTH = 200;

export const ADDRESS_MAX_LENGTH = 500;

export const TAX_IDENTIFIER_MAX_LENGTH = 32;

export const PREFIX_MAX_LENGTH = 8;

export const PREFIX_PATTERN = /^[A-Z0-9]+$/;

export const LEGAL_NAME_HINT =
  "The name the entity is registered under, which is what has to appear on the document rather than whatever it is called day to day.";

export const ADDRESS_HINT =
  "Printed on every document exactly as typed, newlines and all. The country is a separate field because it is read as well as printed — it decides the tax.";

export const TAX_IDENTIFIER_HINT =
  "The entity's own registration. Left blank it is an entity registered for tax nowhere, which is a legitimate thing to be: it can still issue, and its documents charge nothing and say why.";

export const PREFIXES_ARE_PERMANENT =
  "Fixed at creation and absent from the edit form entirely. Changing a prefix once documents have been issued would leave two shapes of number in one series, so there is no endpoint that does it — get these right now.";

export const INVOICE_PREFIX_HINT =
  "The letters in front of every invoice number this entity issues — INV gives INV-2026-000042.";

export const CREDIT_NOTE_PREFIX_HINT =
  "Its own, and never the same as the invoice prefix: the two series are told apart by their numbers alone, so sharing one would produce two documents called the same thing in the same year.";

export const prefixRefusal = (prefix: string): string | null => {
  const trimmed = prefix.trim();

  if (trimmed.length === 0) return "A prefix is required.";

  if (trimmed.length > PREFIX_MAX_LENGTH) {
    return `A prefix is at most ${PREFIX_MAX_LENGTH} characters.`;
  }

  if (!PREFIX_PATTERN.test(trimmed)) {
    return "A prefix is upper-case letters and digits only — no spaces, punctuation or lower case.";
  }

  return null;
};

export const prefixesRefusal = (
  invoiceNumberPrefix: string,
  creditNoteNumberPrefix: string
): string | null =>
  prefixRefusal(invoiceNumberPrefix) ??
  prefixRefusal(creditNoteNumberPrefix) ??
  (invoiceNumberPrefix.trim().toUpperCase() ===
  creditNoteNumberPrefix.trim().toUpperCase()
    ? "Credit notes need their own prefix, so a credit note number cannot be mistaken for an invoice number."
    : null);

export const PREFIX_IN_USE_MESSAGE =
  "Another entity already numbers its documents with that prefix. Prefixes are unique across the installation — two entities sharing one would produce two different documents with the same number.";

export const entityRefusal = (entity: {
  legalName: string;
  address: string;
  countryCode: string;
  taxIdentifier: string;
  invoiceNumberPrefix: string;
  creditNoteNumberPrefix: string;
}): string | null => {
  if (entity.legalName.trim().length === 0) {
    return "A legal name is required — it is the name that appears on every document.";
  }

  if (entity.legalName.trim().length > LEGAL_NAME_MAX_LENGTH) {
    return `A legal name is at most ${LEGAL_NAME_MAX_LENGTH} characters.`;
  }

  if (entity.address.trim().length === 0) return "An address is required.";

  if (entity.address.length > ADDRESS_MAX_LENGTH) {
    return `An address is at most ${ADDRESS_MAX_LENGTH} characters, newlines included.`;
  }

  if (!isSupportedJurisdiction(entity.countryCode)) {
    return "Pick a country this platform can decide tax for. An entity somewhere else could not have the tax on its supplies determined, and every document it issued would have to say so.";
  }

  if (entity.taxIdentifier.trim().length > TAX_IDENTIFIER_MAX_LENGTH) {
    return `A tax identifier is at most ${TAX_IDENTIFIER_MAX_LENGTH} characters.`;
  }

  return prefixesRefusal(
    entity.invoiceNumberPrefix,
    entity.creditNoteNumberPrefix
  );
};

export const CLEARING_THE_REGISTRATION =
  "Clearing this field removes the entity's registration rather than leaving it as it was — an entity that deregisters has no registration to print, and emptying the field is how that is said. Every other field on this form is left alone when it is blank.";

export const EDITABLE_FIELDS =
  "The name, the address, the country, the registration, the currency and whether the entity is offered. The two numbering prefixes are not here and never will be: changing one would leave two shapes of number in one series.";

export const COUNTRY_DECIDES_THE_TAX =
  "The country decides what this entity charges and which rules its cross-border supplies fall under. Changing it changes the tax on documents issued from now on; everything already issued keeps the treatment it was issued under.";

export const DISABLE_RATHER_THAN_DELETE =
  "Disabling withdraws the entity from new customers: nobody new can be pointed at it, and every customer already on it keeps being invoiced by it. This is almost always what is meant by retiring an issuing entity.";

export const DELETE_IS_NARROW =
  "Only possible for an entity that has issued nothing and has no customer pointed at it — a mistake, or an entity set up and never used. The API refuses anything else, because the documents it issued name it and would be left claiming to have been issued by nobody.";

export const ENTITY_IN_USE_MESSAGE =
  "This entity has issued documents or has customers pointed at it, so it can't be deleted — the invoices name it. Disable it instead: that stops anybody new being pointed at it and leaves the record intact.";

export const NUMBERING_POSITION_UNAVAILABLE =
  "Where the series has got to is not shown, because nothing can say it. The number is taken at the moment of issue, the API keeps no counter, and invoices are only listable per organisation — so any figure here would be a guess, and a guessed invoice number is the one thing worse than none.";

export const numberFormat = (prefix: string): string =>
  `${prefix}-YYYY-NNNNNN`;

export const entitiesInReadingOrder = (
  entities: readonly BillingEntity[]
): BillingEntity[] =>
  [...entities].sort((a, b) => {
    if (a.isEnabled !== b.isEnabled) return a.isEnabled ? -1 : 1;

    return a.legalName.localeCompare(b.legalName);
  });

export const selectableEntities = (
  entities: readonly BillingEntity[]
): BillingEntity[] => entities.filter((entity) => entity.isEnabled);

export const entitySentence = (entity: {
  legalName: string;
  countryCode: string;
  currency: CurrencyCode;
}): string =>
  `${entity.legalName}, registered in ${entity.countryCode}, issuing in ${entity.currency}`;

export const NOT_REGISTERED_HINT =
  "Registered for tax nowhere. Its documents charge nothing and say why.";
