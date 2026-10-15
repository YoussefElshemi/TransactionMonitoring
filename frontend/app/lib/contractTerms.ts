import type {
  Commitment,
  ContractDiscount,
  CurrencyCode,
  DiscountType,
  InvoiceAdjustment,
} from "@/types";
import { formatAgreed } from "@/lib/currency";
import { formatPercentage } from "@/lib/pricing";

export const DISCOUNT_TYPE_LABELS: Record<DiscountType, string> = {
  Percentage: "Percentage",
  FixedAmount: "Fixed amount",
};

export const DISCOUNT_TYPE_HINTS: Record<DiscountType, string> = {
  Percentage:
    "A share of whatever is owed when it applies. Because discounts compound, two of ten per cent come to nineteen rather than twenty.",
  FixedAmount:
    "A flat sum off the month's bill, capped at what is owed. A fixed discount larger than the bill reduces it to nothing; it does not become a credit the customer can bank.",
};

export const DISCOUNTS_COMPOUND =
  "Discounts compound. Each comes off what is left after the one before it, oldest first — so two of ten per cent take nineteen per cent in total, not twenty.";

export const DESCRIPTION_MAX_LENGTH = 500;

export const DISCOUNT_DESCRIPTION_HINT =
  "Why this was agreed, and with whom. It is the only record of the reason — the row itself only shows an amount and a date, and somebody reconciling an invoice a year from now is reading this line.";

export const isValidDiscountDescription = (description: string): boolean => {
  const trimmed = description.trim();
  return trimmed.length > 0 && trimmed.length <= DESCRIPTION_MAX_LENGTH;
};

export const discountValueRefusal = (
  discountType: DiscountType,
  value: number
): string | null => {
  if (!Number.isFinite(value)) return "That is not a number.";

  if (value <= 0) {
    return "A discount of nothing is not a discount. To stop one that is running, end it instead.";
  }

  if (discountType === "Percentage" && value > 100) {
    return "A percentage discount cannot exceed 100. Taking more off than was owed would be a contract nobody agreed — a fixed amount is the way to write off a whole month.";
  }

  return null;
};

export const isValidDiscountValue = (
  discountType: DiscountType,
  value: number
): boolean => discountValueRefusal(discountType, value) === null;

export const discountAmount = (
  discount: Pick<ContractDiscount, "discountType" | "value">,
  currency: CurrencyCode | null
): string =>
  discount.discountType === "Percentage"
    ? formatPercentage(discount.value)
    : formatAgreed(discount.value, currency);

export const DISCOUNT_STARTS_HINT =
  "Left blank, it starts now. A discount can be dated from when it was actually agreed, unlike a subscription — the months it applies to are the ones invoiced while it is in force.";

export const DISCOUNT_ENDS_HINT =
  "Left blank, it runs until somebody ends it. An end date is the ordinary way to write a term that was agreed for a fixed period, so nobody has to remember to stop it.";

export const discountWindowRefusal = (
  effectiveFrom: string,
  effectiveTo: string
): string | null => {
  if (effectiveTo.trim().length === 0) return null;

  const to = Date.parse(effectiveTo);
  if (Number.isNaN(to)) return "That end date is not a date.";

  const from =
    effectiveFrom.trim().length === 0 ? Date.now() : Date.parse(effectiveFrom);

  if (Number.isNaN(from)) return "That start date is not a date.";

  if (to <= from) {
    return "A discount cannot end at or before it starts. For a discount that has already finished, set the dates it actually ran between.";
  }

  return null;
};

export type TermState = "scheduled" | "inForce" | "expired";

export const termState = (
  term: { effectiveFrom: string; effectiveTo: string | null },
  now: Date = new Date()
): TermState => {
  if (Date.parse(term.effectiveFrom) > now.getTime()) return "scheduled";

  if (
    term.effectiveTo !== null &&
    Date.parse(term.effectiveTo) <= now.getTime()
  ) {
    return "expired";
  }

  return "inForce";
};

export const TERM_STATE_LABELS: Record<TermState, string> = {
  scheduled: "Scheduled",
  inForce: "In force",
  expired: "Expired",
};

export const isScheduledTerm = (
  term: { effectiveFrom: string; effectiveTo: string | null },
  now: Date = new Date()
): boolean => termState(term, now) === "scheduled";

export const deleteDiscountEffect = (
  discount: Pick<ContractDiscount, "effectiveFrom" | "effectiveTo">,
  now: Date = new Date()
): { removes: boolean; message: string } => {
  if (isScheduledTerm(discount, now)) {
    return {
      removes: true,
      message:
        "This discount has not started, so it is removed outright. Nothing has been invoiced under it and there is nothing to keep.",
    };
  }

  const state = termState(discount, now);

  if (state === "expired") {
    return {
      removes: false,
      message:
        "This discount has already finished, so it stays on the record exactly as it is — including its end date, which is not moved to today. The months it came off were invoiced under it, and it is why those totals are what they are.",
    };
  }

  return {
    removes: false,
    message:
      "This discount is in force, so it is ended rather than deleted: it stops applying from now, and stays on this list with an end date. It has already come off invoices the customer holds, and removing the record would leave those totals unreachable.",
  };
};

export const INCLUDE_EXPIRED_HINT =
  "Off by default. An expired discount still explains the months it came off, so it is kept — it is simply not part of what this customer is on today.";

export type AdjustmentDirection = "credit" | "charge";

export const ADJUSTMENT_DIRECTION_LABELS: Record<AdjustmentDirection, string> =
  {
    credit: "Credit",
    charge: "Charge",
  };

export const ADJUSTMENT_DIRECTION_HINTS: Record<AdjustmentDirection, string> = {
  credit:
    "Takes money off what the customer owes that month — a goodwill gesture, or a month they were charged for an outage.",
  charge:
    "Adds to what the customer owes that month — a migration fee, or work agreed outside the plan.",
};

export const signedAmount = (
  direction: AdjustmentDirection,
  amount: number
): number => (direction === "credit" ? -Math.abs(amount) : Math.abs(amount));

export const directionOf = (
  adjustment: Pick<InvoiceAdjustment, "amount">
): AdjustmentDirection => (adjustment.amount < 0 ? "credit" : "charge");

export const ADJUSTMENT_DESCRIPTION_HINT =
  "What was agreed, and why. This is written onto the customer's invoice as the line's own description, so it is read by them and not only by whoever raised it.";

export const isValidAdjustmentDescription = (description: string): boolean => {
  const trimmed = description.trim();
  return trimmed.length > 0 && trimmed.length <= DESCRIPTION_MAX_LENGTH;
};

export const adjustmentAmountRefusal = (amount: number): string | null => {
  if (!Number.isFinite(amount)) return "That is not a number.";

  if (amount === 0) {
    return "An adjustment of nothing is not an adjustment. It would be a line on the customer's invoice that changes no total.";
  }

  return null;
};

export const isValidAdjustmentAmount = (amount: number): boolean =>
  adjustmentAmountRefusal(amount) === null;

export const isValidMonth = (month: number): boolean =>
  Number.isInteger(month) && month >= 1 && month <= 12;

export const isValidYear = (year: number, now: Date = new Date()): boolean =>
  Number.isInteger(year) && year >= 2000 && year <= now.getFullYear() + 1;

export const MONTH_NAMES = [
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

export const periodLabel = (period: {
  month: number;
  year: number;
}): string => `${MONTH_NAMES[period.month - 1] ?? period.month} ${period.year}`;

export const MONTH_ALREADY_INVOICED =
  "That month has already been invoiced, so a one-off raised against it now would land on no document at all — generation stamps what it finds, and it has already run. Withdraw that invoice and reissue it, or raise this against the next month instead.";

export const isApplied = (
  adjustment: Pick<InvoiceAdjustment, "appliedToInvoiceId">
): boolean => adjustment.appliedToInvoiceId !== null;

export const APPLIED_HINT =
  "Already a line on an invoice the customer holds. It cannot be deleted — the document was totalled with it on, so removing it would leave that total unreachable. Reissue the month instead.";

export const WAITING_HINT =
  "Waiting for that month to close. It is applied when the month is invoiced, and can be deleted freely until then.";

export const INCLUDE_APPLIED_HINT =
  "Off by default. An applied adjustment is a line on a document that has gone out, so it is read from the invoice rather than from here — this list is what is still waiting.";

export const canDeleteAdjustment = (
  adjustment: Pick<InvoiceAdjustment, "appliedToInvoiceId">
): boolean => !isApplied(adjustment);

export const COMMITMENT_IS_NOT_INVOICED_HERE =
  "The commitment itself is agreed and collected outside this system. Monthly invoices only spend it: what a month owes is drawn from the balance and the customer is billed whatever is left over.";

export const DRAWDOWN_IS_DERIVED =
  "What has been drawn is read off the invoices inside the term rather than stored, so withdrawing or regenerating a month gives its share straight back. The balance moving after an invoice is withdrawn is the record working, not drifting.";

export const commitmentAmountRefusal = (amount: number): string | null => {
  if (!Number.isFinite(amount)) return "That is not a number.";

  if (amount <= 0) {
    return "A commitment of nothing is not a commitment.";
  }

  return null;
};

export const isValidCommitmentAmount = (amount: number): boolean =>
  commitmentAmountRefusal(amount) === null;

export const commitmentWindowRefusal = (
  effectiveFrom: string,
  effectiveTo: string
): string | null => {
  if (effectiveTo.trim().length === 0) {
    return "A commitment needs an end date. The term is what the balance is spent over, and one that never ends is a balance nobody ever reconciles.";
  }

  const to = Date.parse(effectiveTo);
  if (Number.isNaN(to)) return "That end date is not a date.";

  const from =
    effectiveFrom.trim().length === 0 ? Date.now() : Date.parse(effectiveFrom);

  if (Number.isNaN(from)) return "That start date is not a date.";

  if (to <= from) return "A commitment cannot end at or before it starts.";

  return null;
};

export const overlapsExisting = (
  proposed: { effectiveFrom: string; effectiveTo: string },
  existing: readonly Pick<Commitment, "effectiveFrom" | "effectiveTo">[]
): boolean => {
  const from = Date.parse(proposed.effectiveFrom);
  const to = Date.parse(proposed.effectiveTo);

  if (Number.isNaN(from) || Number.isNaN(to)) return false;

  return existing.some((term) => {
    const termFrom = Date.parse(term.effectiveFrom);
    const termTo = Date.parse(term.effectiveTo);

    return from < termTo && termFrom < to;
  });
};

export const COMMITMENT_OVERLAP_MESSAGE =
  "This customer already has a commitment covering part of that term. Two terms over the same month would each credit it against a balance promised once, and the month would be billed for neither — end or remove the existing term first, or start this one after it.";

export const COMMITMENT_IN_USE_MESSAGE =
  "Invoices have already drawn against this commitment, so it cannot be deleted — the term is the reason those months came to what they did. Withdraw the invoices that drew on it first, which also gives the balance back.";

export const canDeleteCommitment = (
  commitment: Pick<Commitment, "drawn">
): boolean => commitment.drawn <= 0;

export const drawnFraction = (
  commitment: Pick<Commitment, "amount" | "drawn">
): number => {
  if (commitment.amount <= 0) return 0;

  return Math.min(1, Math.max(0, commitment.drawn / commitment.amount));
};

export const drawdownSentence = (
  commitment: Pick<Commitment, "amount" | "drawn" | "remaining">,
  currency: CurrencyCode | null
): string =>
  `${formatAgreed(commitment.drawn, currency)} drawn of ${formatAgreed(commitment.amount, currency)} — ${formatAgreed(commitment.remaining, currency)} left`;

export const isExhausted = (
  commitment: Pick<Commitment, "remaining">
): boolean => commitment.remaining <= 0;

export const EXHAUSTED_HINT =
  "Spent in full. The term still runs to its end date, and every month until then is invoiced in full — there is nothing left to draw against.";
