import type {
  AccountBalance,
  AccountEntry,
  AccountEntryReason,
  CurrencyCode,
} from "@/types";
import { formatMoney, toCurrencyCode } from "@/lib/currency";

export const ENTRY_REASON_LABELS: Record<AccountEntryReason, string> = {
  Receipt: "payments.reason.receipt",
  Allocation: "payments.reason.allocation",
  Refund: "payments.reason.refund",
  Return: "payments.reason.return",
  CreditNote: "payments.reason.creditNote",
};

export const entryReasonLabelKey = (
  reason: AccountEntryReason | string
): string =>
  ENTRY_REASON_LABELS[reason as AccountEntryReason] ?? String(reason);

export const ENTRY_REASON_HINTS: Record<AccountEntryReason, string> = {
  Receipt: "payments.reasonHint.receipt",
  Allocation: "payments.reasonHint.allocation",
  Refund: "payments.reasonHint.refund",
  Return: "payments.reasonHint.return",
  CreditNote: "payments.reasonHint.creditNote",
};

export const entryReasonHintKey = (
  reason: AccountEntryReason | string
): string => ENTRY_REASON_HINTS[reason as AccountEntryReason] ?? "";

export const entryDirection = (
  entry: Pick<AccountEntry, "amount">
): "in" | "out" => (entry.amount < 0 ? "out" : "in");

export const formatSignedAmount = (
  entry: Pick<AccountEntry, "amount" | "currency">
): string => {
  const currency = toCurrencyCode(entry.currency);
  const magnitude = formatMoney(Math.abs(entry.amount), currency);

  return entry.amount < 0 ? `−${magnitude}` : `+${magnitude}`;
};

export const entriesNewestFirst = (
  entries: readonly AccountEntry[]
): AccountEntry[] =>
  [...entries].sort(
    (left, right) =>
      new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime()
  );

export interface StatementRow {
  entry: AccountEntry;
  balanceAfter: number;
}

export const statementRows = (
  entries: readonly AccountEntry[],
  closingBalances: readonly AccountBalance[]
): StatementRow[] => {
  const running = new Map<string, number>();
  for (const balance of closingBalances) {
    running.set(toCurrencyCode(balance.currency), balance.amount);
  }

  return entriesNewestFirst(entries).map((entry) => {
    const currency = toCurrencyCode(entry.currency);
    const balanceAfter = running.get(currency) ?? 0;

    running.set(currency, balanceAfter - entry.amount);

    return { entry, balanceAfter };
  });
};

export const isInCredit = (
  balance: Pick<AccountBalance, "amount">
): boolean => balance.amount > 0;

export const isInDebit = (balance: Pick<AccountBalance, "amount">): boolean =>
  balance.amount < 0;

export const isUntouched = (
  balance: Pick<AccountBalance, "amount" | "entryCount">
): boolean => balance.amount === 0 && balance.entryCount === 0;

export const balanceMeaningKey = (
  balance: Pick<AccountBalance, "amount" | "entryCount">
): string => {
  if (isUntouched(balance)) return NOTHING_HAS_MOVED;
  if (isInCredit(balance)) return UNAPPLIED_CASH_EXPLAINED;
  if (isInDebit(balance)) return OWED_EXPLAINED;

  return SQUARE_EXPLAINED;
};

export const UNAPPLIED_CASH_EXPLAINED = "payments.unappliedCashExplained";

export const OWED_EXPLAINED = "payments.owedExplained";

export const SQUARE_EXPLAINED = "payments.squareExplained";

export const NOTHING_HAS_MOVED = "payments.nothingHasMoved";

export const BALANCES_NEVER_NET = "payments.balancesNeverNet";

export const LEDGER_IS_APPEND_ONLY = "payments.ledgerIsAppendOnly";

export const balancesInReadingOrder = (
  balances: readonly AccountBalance[]
): AccountBalance[] =>
  [...balances].sort((left, right) =>
    toCurrencyCode(left.currency).localeCompare(toCurrencyCode(right.currency))
  );

export const currenciesOnAccount = (
  balances: readonly AccountBalance[]
): CurrencyCode[] =>
  balancesInReadingOrder(balances).map((balance) =>
    toCurrencyCode(balance.currency)
  );

export const formatBalance = (balance: {
  amount: number;
  currency: string;
}): string => formatMoney(balance.amount, toCurrencyCode(balance.currency));

export const ALLOCATE_EVERYTHING_BY_DEFAULT =
  "reconciliation.allocateEverythingByDefault";

export const REFUND_RECORDS_IT_ONLY = "reconciliation.refundRecordsItOnly";

export const REFUND_REASON_IS_THE_RECORD =
  "reconciliation.refundReasonIsTheRecord";
