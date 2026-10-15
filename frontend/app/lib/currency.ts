import type { CurrencyCode } from "@/types";

export const CURRENCY_CODES: CurrencyCode[] = ["GBP", "EUR", "USD"];

export const CURRENCY_LABELS: Record<CurrencyCode, string> = {
  GBP: "GBP — Pound sterling",
  EUR: "EUR — Euro",
  USD: "USD — US dollar",
};

export const formatMoney = (amount: number, currency: CurrencyCode): string =>
  amount.toLocaleString("en-GB", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

export const formatUnqualified = (amount: number): string =>
  amount.toLocaleString("en-GB", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

export const CURRENCY_NOT_YET_STATED =
  "These amounts carry no currency because this organisation has no billing profile. What was agreed with a customer is agreed in the currency they are billed in, and that is recorded on the profile — until one exists there is nothing to state.";

export const formatAgreed = (
  amount: number,
  currency: CurrencyCode | null
): string =>
  currency === null ? formatUnqualified(amount) : formatMoney(amount, currency);

/**
 * Collection-side endpoints send "Gbp" rather than "GBP", and Intl throws a
 * RangeError on that, so normalise everything from those endpoints here.
 */
export const toCurrencyCode = (currency: string): CurrencyCode => {
  const upper = String(currency).toUpperCase();

  return upper === "GBP" || upper === "EUR" || upper === "USD"
    ? (upper as CurrencyCode)
    : "GBP";
};

export const formatCollected = (amount: number, currency: string): string =>
  formatMoney(amount, toCurrencyCode(currency));

// Intl renders USD as "US$" under en-GB; the API writes "$".
const SYMBOLS: Record<CurrencyCode, string> = {
  GBP: "£",
  EUR: "€",
  USD: "$",
};

export const documentMoney = (
  amount: number,
  currency: CurrencyCode
): string => {
  const formatted = Math.abs(amount).toLocaleString("en-GB", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  return `${amount < 0 ? "-" : ""}${SYMBOLS[currency]}${formatted}`;
};

export const wasConverted = (document: {
  currency: CurrencyCode;
  planCurrency: CurrencyCode;
  conversionRate: number | null;
}): boolean =>
  document.conversionRate !== null &&
  document.planCurrency !== document.currency;

export const conversionSentence = (
  planCurrency: CurrencyCode,
  currency: CurrencyCode,
  conversionRate: number
): string =>
  `Priced in ${planCurrency} and converted at ${conversionRate.toLocaleString(
    "en-GB",
    { minimumFractionDigits: 2, maximumFractionDigits: 6 }
  )} ${currency}/${planCurrency}.`;

export const CURRENCY_ON_THE_PLAN =
  "A plan is priced in one currency and a customer can be billed in another. Where they differ the invoice is converted at the rate in force at the end of the month being billed — not today's — so the document states the plan's currency, the customer's, and the rate that carried one to the other. Regenerating that month next year reaches the same total.";

export const CURRENCY_IS_PART_OF_THE_PRICE =
  "The currency belongs to the version and is fixed with the rest of it. Repricing into another currency publishes a new version, which leaves every invoice already raised naming the currency it was actually priced in.";
