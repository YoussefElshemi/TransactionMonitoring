import type {
  CurrencyCode,
  Invoice,
  InvoiceSettlement,
  Payment,
  PaymentMethodKind,
  PaymentStatus,
} from "@/types";
import { formatMoney, toCurrencyCode } from "@/lib/currency";

export const PAYMENT_STATUSES: PaymentStatus[] = [
  "Pending",
  "Settled",
  "Failed",
  "Returned",
];

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  Pending: "payments.status.pending",
  Settled: "payments.status.settled",
  Failed: "payments.status.failed",
  Returned: "payments.status.returned",
};

export const paymentStatusLabelKey = (status: PaymentStatus | string): string =>
  PAYMENT_STATUS_LABELS[status as PaymentStatus] ?? String(status);

export const PAYMENT_STATUS_HINTS: Record<PaymentStatus, string> = {
  Pending: "payments.statusHint.pending",
  Settled: "payments.statusHint.settled",
  Failed: "payments.statusHint.failed",
  Returned: "payments.statusHint.returned",
};

export const paymentStatusHintKey = (status: PaymentStatus | string): string =>
  PAYMENT_STATUS_HINTS[status as PaymentStatus] ?? "";

export const isInFlight = (payment: Pick<Payment, "paymentStatus">): boolean =>
  payment.paymentStatus === "Pending";

export const isSettledPayment = (
  payment: Pick<Payment, "paymentStatus">
): boolean => payment.paymentStatus === "Settled";

export const isReversed = (payment: Pick<Payment, "paymentStatus">): boolean =>
  payment.paymentStatus === "Returned";

export const hasFailed = (payment: Pick<Payment, "paymentStatus">): boolean =>
  payment.paymentStatus === "Failed";

// Returned payments keep their receiptNumber but must not offer a download.
export const canDownloadReceipt = (
  payment: Pick<Payment, "paymentStatus" | "receiptNumber">
): boolean => payment.paymentStatus === "Settled" && payment.receiptNumber !== null;

export const RECEIPT_ONLY_WHEN_SETTLED = "payments.receiptOnlyWhenSettled";

export const decisiveDate = (
  payment: Pick<
    Payment,
    "paymentStatus" | "receivedAt" | "settledAt" | "returnedAt"
  >
): { labelKey: string; value: string } => {
  if (payment.paymentStatus === "Returned" && payment.returnedAt !== null) {
    return { labelKey: "payments.dated.reversed", value: payment.returnedAt };
  }

  if (payment.paymentStatus === "Settled" && payment.settledAt !== null) {
    return { labelKey: "payments.dated.settled", value: payment.settledAt };
  }

  return { labelKey: "payments.dated.requested", value: payment.receivedAt };
};

export type PaymentStatusFilter = "all" | PaymentStatus;

export const PAYMENT_STATUS_FILTERS: PaymentStatusFilter[] = [
  "all",
  ...PAYMENT_STATUSES,
];

export const PAYMENT_STATUS_FILTER_LABELS: Record<
  PaymentStatusFilter,
  string
> = {
  all: "payments.filter.all",
  ...PAYMENT_STATUS_LABELS,
};

export const statusQueryValue = (
  filter: PaymentStatusFilter
): PaymentStatus | null => (filter === "all" ? null : filter);

export const PAYMENT_METHOD_KIND_LABELS: Record<PaymentMethodKind, string> = {
  Card: "payments.kind.card",
  DirectDebit: "payments.kind.directDebit",
  BankTransfer: "payments.kind.bankTransfer",
};

export const paymentMethodKindLabelKey = (
  kind: PaymentMethodKind | string
): string =>
  PAYMENT_METHOD_KIND_LABELS[kind as PaymentMethodKind] ?? String(kind);

export const paymentMethodDescription = (
  payment: Pick<Payment, "paymentMethodKind" | "paymentMethodLabel">
): { label: string } | { labelKey: string } =>
  payment.paymentMethodLabel !== null
    ?
      { label: payment.paymentMethodLabel }
    : { labelKey: paymentMethodKindLabelKey(payment.paymentMethodKind) };

export const formatPaymentAmount = (
  payment: Pick<Payment, "amount" | "currency">
): string => formatMoney(payment.amount, toCurrencyCode(payment.currency));

export const currenciesOnPayments = (
  payments: readonly Pick<Payment, "currency">[]
): CurrencyCode[] =>
  [...new Set(payments.map((payment) => toCurrencyCode(payment.currency)))].sort();

export const hasSettlement = (resource: {
  settlement?: InvoiceSettlement;
}): resource is { settlement: InvoiceSettlement } =>
  resource.settlement !== undefined && resource.settlement !== null;

export const DRAFT_HAS_NO_SETTLEMENT = "billing.draftHasNoSettlement";

export const SETTLED_IS_NOT_PAID = "billing.settledIsNotPaid";

export const settlementRoute = (
  settlement: Pick<InvoiceSettlement, "allocatedAmount" | "creditedAmount">
): "paid" | "credited" | "both" | "neither" => {
  const paid = settlement.allocatedAmount > 0;
  const credited = settlement.creditedAmount > 0;

  if (paid && credited) return "both";
  if (paid) return "paid";
  if (credited) return "credited";

  return "neither";
};

export const SETTLEMENT_ROUTE_HINTS: Record<
  ReturnType<typeof settlementRoute>,
  string
> = {
  paid: "billing.settledByPayment",
  credited: "billing.settledByCreditNote",
  both: "billing.settledByBoth",
  neither: "billing.settledOnIssue",
};

export const hasOutstanding = (
  settlement: Pick<InvoiceSettlement, "outstandingAmount">
): boolean => settlement.outstandingAmount > 0;

export const canCollect = (
  invoice: Pick<Invoice, "invoiceStatus">,
  settlement: InvoiceSettlement | undefined
): boolean =>
  invoice.invoiceStatus === "Issued" &&
  settlement !== undefined &&
  settlement !== null &&
  hasOutstanding(settlement);

export const OUTSTANDING_IS_FLOORED = "billing.outstandingIsFloored";

export const outstandingSummary = (
  settlement: InvoiceSettlement,
  currency: CurrencyCode
): { labelKey: string } | { amount: string } =>
  settlement.isSettled
    ? { labelKey: "billing.nothingOutstanding" }
    : { amount: formatMoney(settlement.outstandingAmount, currency) };

export type CollectionRefusal =
  | "no-payment-method"
  | "invoice-settled"
  | "currency-mismatch"
  | "insufficient-balance";

const REFUSAL_TYPES: Record<string, CollectionRefusal> = {
  NoPaymentMethodException: "no-payment-method",
  InvoiceSettledException: "invoice-settled",
  CurrencyMismatchException: "currency-mismatch",
  InsufficientBalanceException: "insufficient-balance",
};

export const readCollectionRefusal = (
  body: unknown
): CollectionRefusal | null => {
  const problem = body as { type?: string } | null;
  if (!problem?.type) return null;

  return REFUSAL_TYPES[problem.type] ?? null;
};

export const COLLECTION_REFUSAL_TITLES: Record<CollectionRefusal, string> = {
  "no-payment-method": "payments.refusal.noMethodTitle",
  "invoice-settled": "payments.refusal.settledTitle",
  "currency-mismatch": "payments.refusal.currencyTitle",
  "insufficient-balance": "payments.refusal.balanceTitle",
};

export const COLLECTION_REFUSAL_HINTS: Record<CollectionRefusal, string> = {
  "no-payment-method": "payments.refusal.noMethodHint",
  "invoice-settled": "payments.refusal.settledHint",
  "currency-mismatch": "payments.refusal.currencyHint",
  "insufficient-balance": "payments.refusal.balanceHint",
};

export const paymentStatusTone = (
  status: PaymentStatus
): "settled" | "inflight" | "elevated" | "critical" => {
  switch (status) {
    case "Settled":
      return "settled";
    case "Pending":
      return "inflight";
    case "Returned":
      return "elevated";
    case "Failed":
      return "critical";
    default:
      return "inflight";
  }
};
