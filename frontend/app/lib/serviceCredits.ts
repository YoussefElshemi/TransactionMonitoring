import type { ServiceCredit } from "@/types";
import { formatMoney, toCurrencyCode } from "@/lib/currency";
import { formatUptime } from "@/lib/availability";

export { toCurrencyCode };

export const formatCreditAmount = (
  credit: Pick<ServiceCredit, "amount" | "currency">
): string => formatMoney(credit.amount, toCurrencyCode(credit.currency));

export const metTheTarget = (
  credit: Pick<ServiceCredit, "creditPercentage">
): boolean => credit.creditPercentage <= 0;

const MONTH_NAMES = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

export const monthLabel = (
  credit: Pick<ServiceCredit, "month" | "year">
): string => {
  const name = MONTH_NAMES[credit.month - 1];

  return name === undefined
    ? `${credit.month}/${credit.year}`
    : `${name} ${credit.year}`;
};

export const monthKey = (
  credit: Pick<ServiceCredit, "month" | "year">
): number => credit.year * 12 + credit.month;

export const newestMonthFirst = (
  credits: readonly ServiceCredit[]
): ServiceCredit[] => [...credits].sort((a, b) => monthKey(b) - monthKey(a));

export const creditSentence = (credit: ServiceCredit): string => {
  const measured = formatUptime(credit.measuredUptimePercentage);
  const target = formatUptime(credit.availabilityTarget);

  if (metTheTarget(credit)) {
    return `${measured} against a target of ${target}. Met — nothing is owed.`;
  }

  return `${measured} against a target of ${target}. ${formatUptime(
    credit.creditPercentage
  )} of the month's net charge credited, coming to ${formatCreditAmount(credit)}.`;
};

export const creditTone = (
  credit: Pick<ServiceCredit, "creditPercentage">
): "settled" | "critical" => (metTheTarget(credit) ? "settled" : "critical");

export const hasCreditNote = (
  credit: Pick<ServiceCredit, "creditNoteId">
): boolean => credit.creditNoteId !== null;

export const paperworkSentence = (credit: ServiceCredit): string => {
  if (metTheTarget(credit)) {
    return "Nothing was owed, so no credit note was raised.";
  }

  if (credit.creditNoteId !== null) {
    return "A credit note has been raised for this month. The money has been given back.";
  }

  if (credit.invoiceId === null) {
    return "No invoice was found for this month, so there was nothing to work a credit off. The percentage stands; the amount does not.";
  }

  return "Measured and owed, but no credit note has been raised yet. The money has not been given back.";
};

export const WHY_MET_MONTHS_ARE_LISTED =
  "Every month measured is listed, including the ones that met their target and cost nothing. A run of met months beside the missed ones is what makes this a record rather than a complaints log.";

export const ONLY_MISSED_LABEL = "Hide months that met their target";

export const ISSUING_IS_THE_EXCEPTION =
  "The monthly run measures and credits every month on its own. Use this only for a month it could not do — a customer with no invoice, or a run that failed halfway.";

export const ALREADY_MEASURED_MESSAGE =
  "This month has already been measured and credited. A credit note issued twice for one bad month is money that does not come back, so the run and this form share the same guard.";

export const issueRefusal = (
  draft: { month: string; year: string },
  now: Date = new Date()
): string | null => {
  const month = Number(draft.month.trim());
  const year = Number(draft.year.trim());

  if (!Number.isInteger(month) || month < 1 || month > 12) {
    return "A month is a number from 1 to 12.";
  }

  if (!Number.isInteger(year) || year < 2000 || year > 2200) {
    return "That is not a year this platform has run in.";
  }

  const key = year * 12 + month;
  const currentKey = now.getUTCFullYear() * 12 + (now.getUTCMonth() + 1);

  if (key >= currentKey) {
    return "That month is not over yet. Measuring a month before it ends counts the days that have not happened as though nothing was served on them.";
  }

  return null;
};
