import type { BillingProfile, CurrencyCode } from "@/types";
import { isSupportedJurisdiction, registrationPrefix } from "@/lib/tax";

export const LEGAL_NAME_MAX_LENGTH = 200;
export const CONTACT_NAME_MAX_LENGTH = 200;
export const ADDRESS_MAX_LENGTH = 500;
export const TAX_IDENTIFIER_MAX_LENGTH = 32;

export const PAYMENT_TERMS_MAX_DAYS = 365;

export const LEGAL_NAME_HINT =
  "The name the customer is registered under. It goes on the document, so it is theirs rather than the organisation's name in this console — the two are often different, and only one of them is what their finance team will match a payment against.";

export const CONTACT_HINT =
  "Who to send the invoice to. Not necessarily anybody with a login here: accounts payable rarely is.";

export const COUNTRY_HINT =
  "The one part of the address the platform reads rather than only prints. Together with the issuing entity's country it decides how the supply is taxed, so it is the customer's place of establishment rather than wherever they want post sent.";

export const CURRENCY_HINT =
  "What this customer is billed in. Where their plan is priced in something else the invoice is converted at the rate in force at the end of the month billed, and states both.";

export const PAYMENT_TERMS_HINT =
  "Days from issue to due, snapshotted onto each invoice as it is issued — so renegotiating terms does not make an invoice already sent retrospectively late. Zero means due on issue.";

export const profileRefusal = (profile: {
  billingEntityId: string;
  legalName: string;
  contactName: string;
  contactEmail: string;
  address: string;
  countryCode: string;
  taxIdentifier: string;
  paymentTerms: string;
}): string | null => {
  if (profile.billingEntityId.trim().length === 0) {
    return "Pick which of the platform's entities invoices this customer — it decides the numbering series their invoices come out of and half of how their tax is worked out.";
  }

  if (profile.legalName.trim().length === 0) {
    return "A legal name is required — it is who the document is addressed to.";
  }

  if (profile.legalName.trim().length > LEGAL_NAME_MAX_LENGTH) {
    return `A legal name is at most ${LEGAL_NAME_MAX_LENGTH} characters.`;
  }

  if (profile.contactName.trim().length === 0) {
    return "A contact name is required.";
  }

  if (profile.contactName.trim().length > CONTACT_NAME_MAX_LENGTH) {
    return `A contact name is at most ${CONTACT_NAME_MAX_LENGTH} characters.`;
  }

  if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(profile.contactEmail.trim())) {
    return "That is not an email address, and the invoice has nowhere to go without one.";
  }

  if (profile.address.trim().length === 0) return "An address is required.";

  if (profile.address.length > ADDRESS_MAX_LENGTH) {
    return `An address is at most ${ADDRESS_MAX_LENGTH} characters, newlines included.`;
  }

  if (!isSupportedJurisdiction(profile.countryCode)) {
    return "Pick a country this platform can decide tax for. A customer somewhere else could not have the tax on their invoices determined, so the API refuses it rather than defaulting to a rate nobody chose.";
  }

  if (profile.taxIdentifier.trim().length > TAX_IDENTIFIER_MAX_LENGTH) {
    return `A tax identifier is at most ${TAX_IDENTIFIER_MAX_LENGTH} characters.`;
  }

  const terms = Number(profile.paymentTerms.trim());

  if (
    profile.paymentTerms.trim().length === 0 ||
    !Number.isInteger(terms) ||
    terms < 0 ||
    terms > PAYMENT_TERMS_MAX_DAYS
  ) {
    return `Payment terms are a whole number of days between 0 and ${PAYMENT_TERMS_MAX_DAYS}.`;
  }

  return null;
};

export const isMissingProfile = (response: { status: number }): boolean =>
  response.status === 409;

export const NO_PROFILE_HEADLINE = "No billing details yet";

export const NO_PROFILE_DETAIL =
  "This organisation cannot be invoiced until it has some. An invoice with no addressee, no country and no terms is not a document a finance team can post or a customer can pay — so `POST /invoices` refuses rather than inventing any of the three. Nothing is wrong here; nobody has filled this in yet.";

export const SAVING_IS_AN_UPSERT =
  "There is one set of billing details per organisation, so saving this creates them the first time and replaces them every time after. There is no second copy and no history — an invoice already issued carries its own snapshot of all of this and does not change when these do.";

export const hasRegistration = (
  profile: Pick<BillingProfile, "taxIdentifier">
): boolean => (profile.taxIdentifier ?? "").trim().length > 0;

export const isVerified = (
  profile: Pick<BillingProfile, "taxIdentifier" | "taxIdentifierVerifiedAt">
): boolean =>
  hasRegistration(profile) && profile.taxIdentifierVerifiedAt !== null;

export const isUnverified = (
  profile: Pick<BillingProfile, "taxIdentifier" | "taxIdentifierVerifiedAt">
): boolean =>
  hasRegistration(profile) && profile.taxIdentifierVerifiedAt === null;

export type RegistrationState = "none" | "unverified" | "verified";

export const registrationState = (
  profile: Pick<BillingProfile, "taxIdentifier" | "taxIdentifierVerifiedAt">
): RegistrationState => {
  if (!hasRegistration(profile)) return "none";

  return profile.taxIdentifierVerifiedAt === null ? "unverified" : "verified";
};

export const REGISTRATION_STATE_LABELS: Record<RegistrationState, string> = {
  none: "No registration",
  unverified: "Not verified",
  verified: "Verified",
};

export const REGISTRATION_STATE_HINTS: Record<RegistrationState, string> = {
  none: "No registration on file. The customer is treated as one, which for a business elsewhere in the EU means their invoices are charged tax at their own country's rate.",
  unverified:
    "On file and never confirmed. The tax determination ignores it entirely, so the invoice charges tax exactly as though the field were empty — this is not a smaller version of verified, it is the same as having none.",
  verified:
    "Confirmed, and the only state that moves the tax. A business elsewhere in the EU is reverse-charged from here: no tax on the document, and the customer accounts for it under this registration.",
};

export const wouldClearVerification = (
  profile: Pick<
    BillingProfile,
    "taxIdentifier" | "countryCode" | "taxIdentifierVerifiedAt"
  > | null,
  next: { taxIdentifier: string; countryCode: string }
): boolean => {
  if (profile === null) return false;
  if (profile.taxIdentifierVerifiedAt === null) return false;

  const normalise = (value: string) =>
    value.replace(/\s+/g, "").toUpperCase();

  return (
    normalise(profile.taxIdentifier ?? "") !==
      normalise(next.taxIdentifier) ||
    normalise(profile.countryCode) !== normalise(next.countryCode)
  );
};

export const canVerify = (
  profile: Pick<
    BillingProfile,
    "taxIdentifier" | "taxIdentifierVerifiedAt"
  > | null
): boolean => profile !== null && isUnverified(profile);

export const expectedPrefixHint = (countryCode: string): string =>
  `A registration issued in ${countryCode.toUpperCase()} starts with ${registrationPrefix(countryCode)}.`;

export const issuedBySentence = (
  profile: Pick<BillingProfile, "billingEntityLegalName">
): string =>
  profile.billingEntityLegalName === null
    ? "Issued by an entity that could not be resolved"
    : `Issued by ${profile.billingEntityLegalName}`;

export const billingCurrency = (
  profile: Pick<BillingProfile, "currency"> | null
): CurrencyCode | null => profile?.currency ?? null;
