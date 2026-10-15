import type { CurrencyCode, Invoice, InvoiceStatus } from "@/types";

export const INVOICE_STATUS_LABELS: Record<InvoiceStatus, string> = {
  Draft: "Draft",
  Issued: "Issued",
};

export const INVOICE_STATUS_HINTS: Record<InvoiceStatus, string> = {
  Draft:
    "Arithmetic nobody has been handed. It has no number and no due date, it cannot be late, and it can be withdrawn and regenerated as often as you like — which is exactly what to do with a month that was billed against the wrong terms.",
  Issued:
    "A document the customer holds and their finance team can post. It took the next number in its entity's series, and nothing edits it now: a correction is a credit note, which is its own document saying what was given back and why.",
};

export const DRAFT_HAS_NO_NUMBER = "Draft — not yet issued";

export const DRAFT_IS_NOT_A_DOCUMENT =
  "A draft is not a document. No number, no due date, nothing to be late by, and nobody has been sent it. It can be withdrawn and regenerated as often as you like — so a month billed against the wrong plan, the wrong discounts or the wrong rate is fixed by deleting this and generating it again.";

export const ISSUED_IS_IMMUTABLE =
  "Issuing is one-way. It takes the next number in the series and stamps the issue and due dates, and after it nothing edits the invoice, nothing withdraws it and nothing renumbers it. A correction is a credit note.";

export const NUMBERING_IS_GAP_FREE =
  "Numbering is gap-free per issuing entity, because most jurisdictions require it. An invoice raised in error keeps its number and is credited in full rather than disappearing — a hole in the series is indistinguishable from a document somebody took out of it.";

export const NUMBER_COMES_FROM_THE_SERIES =
  "The number is the next one in the issuing entity's series, taken at the moment of issue — not reserved when the draft was generated. Two drafts issued in the other order come out with the other numbers.";

export const isDraft = (invoice: Pick<Invoice, "invoiceStatus">): boolean =>
  invoice.invoiceStatus === "Draft";

export const isIssued = (invoice: Pick<Invoice, "invoiceStatus">): boolean =>
  invoice.invoiceStatus === "Issued";

export const canIssue = (invoice: Pick<Invoice, "invoiceStatus">): boolean =>
  isDraft(invoice);

export const canDelete = (invoice: Pick<Invoice, "invoiceStatus">): boolean =>
  isDraft(invoice);

export const canCredit = (invoice: Pick<Invoice, "invoiceStatus">): boolean =>
  isIssued(invoice);

export const ALREADY_ISSUED_MESSAGE =
  "This invoice has already been issued, so it cannot be issued again — it has a number, and taking a second one would leave a hole in the series. Reload the screen; somebody issued it since this one was opened.";

export const CANNOT_DELETE_ISSUED_MESSAGE =
  "This invoice has been issued, so it cannot be deleted. The customer holds it and its number is part of a gap-free series. Raise a credit note against it instead — that records what was given back and why, which deleting it would not.";

export const NO_BILLING_PROFILE_MESSAGE =
  "This organisation has no billing profile, so there is nobody to address the invoice to and no terms to date it by. Record its billing details first — the panel is above this one.";

export const NO_EXCHANGE_RATE_MESSAGE =
  "The plan is priced in a currency this customer is not billed in, and no exchange rate was in force at the end of the month being billed. Record one on the exchange rates screen, dated from before the end of that month, then generate the invoice again.";

export const isOverdue = (
  invoice: Pick<Invoice, "invoiceStatus" | "dueAt" | "isProcessed">,
  now: Date = new Date()
): boolean => {
  if (invoice.invoiceStatus !== "Issued") return false;
  if (invoice.isProcessed) return false;
  if (invoice.dueAt === null) return false;

  const due = Date.parse(invoice.dueAt);
  if (Number.isNaN(due)) return false;

  return due < now.getTime();
};

export const daysOverdue = (
  invoice: Pick<Invoice, "invoiceStatus" | "dueAt" | "isProcessed">,
  now: Date = new Date()
): number => {
  if (!isOverdue(invoice, now)) return 0;

  const due = Date.parse(invoice.dueAt as string);

  return Math.floor((now.getTime() - due) / 86_400_000);
};

export const paymentTermsSentence = (paymentTerms: number): string =>
  paymentTerms === 0
    ? "Due on issue"
    : `Due ${paymentTerms} day${paymentTerms === 1 ? "" : "s"} from issue`;

export const predatesParties = (
  invoice: Pick<Invoice, "supplier" | "customer">
): boolean => invoice.supplier === null && invoice.customer === null;

export const NO_PARTIES_HEADLINE = "This document predates its parties";

export const NO_PARTIES_DETAIL =
  "It was raised before an invoice recorded who issued it and who it was addressed to. The total and its working are exactly as they were; there is simply no supplier or customer block to show, and nothing since can supply one — the parties are a snapshot of the moment of issue, not a lookup.";

export const isTaxLine = (line: { invoiceLineType: string }): boolean =>
  line.invoiceLineType === "Tax";

export const ZERO_TAX_IS_LOAD_BEARING =
  "A tax line at zero is kept and its description is the reason. On a reverse-charged document that sentence is what the customer's finance team accounts for the tax against; on an out-of-scope one it is the difference between tax that was not due and tax somebody forgot.";

export const totalsBalance = (
  invoice: Pick<Invoice, "netAmount" | "taxAmount" | "amount">
): boolean =>
  Math.abs(invoice.netAmount + invoice.taxAmount - invoice.amount) < 0.005;

export const TOTALS_DISAGREE =
  "The net and the tax on this document do not come to its total. All three are stated by the invoice rather than computed here, so one of them has changed since it was issued — quote the invoice number rather than any of the figures.";

export const invoiceLabel = (
  invoice: Pick<Invoice, "invoiceNumber" | "invoiceStatus">
): string => invoice.invoiceNumber ?? DRAFT_HAS_NO_NUMBER;

export const exportFileName = (
  invoice: Pick<Invoice, "invoiceId" | "invoiceNumber">
): string => `${invoice.invoiceNumber ?? invoice.invoiceId}.pdf`;

export type InvoiceStatusFilter = "all" | InvoiceStatus;

export const INVOICE_STATUS_FILTER_LABELS: Record<InvoiceStatusFilter, string> =
  {
    all: "All",
    Draft: "Drafts",
    Issued: "Issued",
  };

export const matchesStatusFilter = (
  invoice: Pick<Invoice, "invoiceStatus">,
  filter: InvoiceStatusFilter
): boolean => filter === "all" || invoice.invoiceStatus === filter;

export const invoicesInWorkingOrder = (invoices: readonly Invoice[]): Invoice[] =>
  [...invoices].sort((a, b) => {
    if (a.invoiceStatus !== b.invoiceStatus) {
      return a.invoiceStatus === "Draft" ? -1 : 1;
    }

    return b.year - a.year || b.month - a.month;
  });

export const currenciesPresent = (
  invoices: readonly { currency: CurrencyCode }[]
): CurrencyCode[] => [...new Set(invoices.map((invoice) => invoice.currency))];
