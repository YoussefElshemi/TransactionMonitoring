import type { CreditNote, Invoice } from "@/types";
import { formatMoney } from "@/lib/currency";

export const REASON_MAX_LENGTH = 500;

export const REASON_HINT =
  "Why the credit was raised, in your own words. Required, and it goes on the document the customer receives — a credit nobody had to explain is one nobody can audit later.";

export const AMOUNT_HINT =
  "Left blank, this credits everything still uncredited on the invoice — line for line where nothing has been credited off it yet, and as a single summary line where something has. Give an amount to credit part of it.";

export const AMOUNTS_ARE_POSITIVE =
  "The amounts on a credit note are positive. The document credits; saying so in the sign as well would subtract the total twice.";

export const TAX_FOLLOWS_THE_INVOICE =
  "Credited on the terms the original invoice was issued under. A document that reverse-charged has no tax to give back, and one issued at twenty per cent gives back twenty — the treatment is not decided again for the credit.";

export const isValidReason = (reason: string): boolean => {
  const trimmed = reason.trim();

  return trimmed.length > 0 && trimmed.length <= REASON_MAX_LENGTH;
};

export const creditRefusal = (
  reason: string,
  amount: string,
  remaining?: number
): string | null => {
  if (reason.trim().length === 0) {
    return "Say why this credit is being raised. It goes on the document, and it is the only record of what happened.";
  }

  if (reason.trim().length > REASON_MAX_LENGTH) {
    return `A reason is at most ${REASON_MAX_LENGTH} characters.`;
  }

  if (amount.trim().length === 0) return null;

  const value = Number(amount.trim());

  if (!Number.isFinite(value) || value <= 0) {
    return "A credit of nothing is not a credit. Leave the amount blank to credit whatever is left, or give a positive figure.";
  }

  if (remaining !== undefined && value > remaining + 0.005) {
    return "That is more than is left uncredited on this invoice. Giving back more than was ever charged is not a correction — it is a payment, and it belongs somewhere that records why money left.";
  }

  return null;
};

export const CREDIT_EXCEEDS_INVOICE_MESSAGE =
  "That is more than is still uncredited on this invoice. Another credit note may have been raised against it since this screen was opened — reload and check what is left.";

export const CANNOT_CREDIT_DRAFT_MESSAGE =
  "This invoice is still a draft, so there is nothing to credit. Nobody has been handed it and nothing has been charged — delete it and generate the month again instead.";

export const creditedAmount = (
  creditNotes: readonly Pick<CreditNote, "netAmount">[]
): number =>
  creditNotes.reduce((total, creditNote) => total + creditNote.netAmount, 0);

export const uncreditedAmount = (
  invoice: Pick<Invoice, "netAmount">,
  creditNotes: readonly Pick<CreditNote, "netAmount">[]
): number => Math.max(0, invoice.netAmount - creditedAmount(creditNotes));

export const isFullyCredited = (
  invoice: Pick<Invoice, "netAmount">,
  creditNotes: readonly Pick<CreditNote, "netAmount">[]
): boolean => uncreditedAmount(invoice, creditNotes) < 0.005;

export const remainingSentence = (
  invoice: Pick<Invoice, "netAmount" | "currency">,
  creditNotes: readonly Pick<CreditNote, "netAmount">[]
): string => {
  const remaining = uncreditedAmount(invoice, creditNotes);

  if (remaining < 0.005) {
    return "This invoice has been credited in full. It keeps its number and its place in the series; there is nothing left to credit.";
  }

  return `${formatMoney(remaining, invoice.currency)} of ${formatMoney(
    invoice.netAmount,
    invoice.currency
  )} net is still uncredited.`;
};

export const creditNotesNewestFirst = (
  creditNotes: readonly CreditNote[]
): CreditNote[] =>
  [...creditNotes].sort(
    (a, b) => Date.parse(b.issuedAt) - Date.parse(a.issuedAt)
  );

export const linesInOrder = <T extends { sortOrder: number }>(
  lines: readonly T[]
): T[] => [...lines].sort((a, b) => a.sortOrder - b.sortOrder);

export const creditNoteHeading = (creditNote: {
  creditNoteNumber: string;
  invoiceNumber: string | null;
}): string =>
  creditNote.invoiceNumber === null
    ? `${creditNote.creditNoteNumber}, against an invoice raised before numbering`
    : `${creditNote.creditNoteNumber}, against ${creditNote.invoiceNumber}`;

export const totalsBalance = (
  creditNote: Pick<CreditNote, "netAmount" | "taxAmount" | "amount">
): boolean =>
  Math.abs(
    creditNote.netAmount + creditNote.taxAmount - creditNote.amount
  ) < 0.005;

export const exportFileName = (
  creditNote: Pick<CreditNote, "creditNoteNumber">
): string => `${creditNote.creditNoteNumber}.pdf`;

export const NO_CREDIT_NOTES_DETAIL =
  "Nothing has been credited against this invoice, which is the ordinary state — a credit note exists only where something had to be given back.";

export const CREDIT_IS_IN_THE_INVOICE_CURRENCY =
  "Credited in the currency the invoice was issued in. It is not converted again — the rate that carried the plan into this currency was fixed when the invoice was issued, and using a later one would give back a different amount from the one charged.";
