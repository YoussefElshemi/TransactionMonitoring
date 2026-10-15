"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Wallet } from "lucide-react";

import type { AccountBalance, AccountEntry } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { toCurrencyCode } from "@/lib/currency";
import {
  BALANCES_NEVER_NET,
  LEDGER_IS_APPEND_ONLY,
  balanceMeaningKey,
  balancesInReadingOrder,
  entryDirection,
  formatBalance,
  formatSignedAmount,
  isInCredit,
  isInDebit,
  entryReasonHintKey,
  entryReasonLabelKey,
  isUntouched,
  statementRows,
} from "@/lib/account";
import { usePermissions } from "@/context/PermissionsContext";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useTranslation } from "@/context/LocalizationContext";

export function AccountPanel({
  organizationId,
  onUnauthorized,
  invoiceHref,
  creditNoteHref,
  onReconcile,
}: {
  organizationId: string;
  onUnauthorized: () => void;
  invoiceHref?: (invoiceId: string) => string | null;
  creditNoteHref?: (creditNoteId: string) => string | null;
  onReconcile?: (balance: AccountBalance) => void;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();

  const mayRead = holds("payments:read");

  const [balances, setBalances] = useState<AccountBalance[]>([]);
  const [entries, setEntries] = useState<AccountEntry[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const fetchAccount = useCallback(async () => {
    setLoading(true);

    try {
      const [balanceResponse, entryResponse] = await Promise.all([
        apiFetch(`${API_BASE}/organizations/${organizationId}/account`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }),
        apiFetch(
          `${API_BASE}/organizations/${organizationId}/account/entries?PageNumber=${pageNumber}&PageSize=25`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        ),
      ]);

      if (balanceResponse.ok) {
        setBalances(await unwrap<AccountBalance[]>(balanceResponse));
      } else {
        await reportApiError(balanceResponse, {
          fallback: t("payments.accountCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-account",
        });
      }

      if (entryResponse.ok) {
        const page = await unwrapPage<AccountEntry>(entryResponse);
        setEntries(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(entryResponse, {
          fallback: t("payments.accountCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-account",
        });
      }
    } catch {
      toast.error(t("payments.accountCouldNotBeRead"), {
        id: "error-fetching-account",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchAccount();
    else setLoading(false);
  }, [mayRead, fetchAccount]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")}{" "}
          <span className="font-mono text-xs">payments:read</span>.
        </p>
      </div>
    );
  }

  const ordered = balancesInReadingOrder(balances);
  const showRunning = pageNumber === 1;
  const rows = statementRows(entries, balances);

  return (
    <div className="space-y-4">
      <div className="flex items-start gap-2">
        <Wallet className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("payments.accountBalance")}
          </p>
          <p className="mt-1 max-w-2xl text-sm text-slate-500">
            {t("payments.whatHasMovedBetweenUs")}
          </p>
        </div>
      </div>

      {loading ? (
        <Skeleton className="h-24 w-full rounded-xl" />
      ) : ordered.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-card p-6">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("payments.nothingOnAccount")}
          </p>
          <p className="mt-2 max-w-xl text-sm text-slate-500">
            {t("payments.noMoneyHasMovedYet")}
          </p>
        </div>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {ordered.map((balance) => (
            <BalanceCard
              key={balance.currency}
              balance={balance}
              onReconcile={onReconcile}
            />
          ))}
        </div>
      )}

      {ordered.length > 1 && (
        <p className="text-xs text-slate-400">{t(BALANCES_NEVER_NET)}</p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-36">{t("payments.when")}</TableHead>
              <TableHead className="w-40">{t("payments.movement")}</TableHead>
              <TableHead>{t("common.description")}</TableHead>
              <TableHead className="w-20">{t("common.currency")}</TableHead>
              <TableHead className="w-32 text-right">
                {t("common.amount")}
              </TableHead>
              {showRunning && (
                <TableHead className="w-32 text-right">
                  {t("payments.balance")}
                </TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: showRunning ? 6 : 5 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={showRunning ? 6 : 5}
                  className="py-10 text-center"
                >
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("payments.nothingOnTheLedger")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {t("payments.everyMovementAppearsHere")}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              rows.map(({ entry, balanceAfter }) => {
                const direction = entryDirection(entry);
                const currency = toCurrencyCode(entry.currency);

                const href =
                  entry.invoiceId !== null
                    ? (invoiceHref?.(entry.invoiceId) ?? null)
                    : entry.creditNoteId !== null
                      ? (creditNoteHref?.(entry.creditNoteId) ?? null)
                      : null;

                return (
                  <TableRow
                    key={entry.accountEntryId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell className="text-sm text-slate-700">
                      <LocalTime value={entry.createdAt} dateOnly />
                    </TableCell>

                    <TableCell className="text-xs text-slate-600">
                      {t(entryReasonLabelKey(entry.reason))}
                      <span
                        className="block text-[11px] text-slate-400"
                        title={
                          entryReasonHintKey(entry.reason)
                            ? t(entryReasonHintKey(entry.reason))
                            : undefined
                        }
                      >
                        {direction === "in"
                          ? t("payments.onto")
                          : t("payments.off")}
                      </span>
                    </TableCell>

                    <TableCell className="text-sm text-slate-700">
                      {entry.description}
                      {href !== null && (
                        <a
                          href={href}
                          className="ml-2 text-xs text-inflight-700 underline underline-offset-2"
                        >
                          {entry.invoiceId !== null
                            ? t("payments.openInvoice")
                            : t("payments.openCreditNote")}
                        </a>
                      )}
                    </TableCell>

                    <TableCell className="font-mono text-xs text-slate-500">
                      {currency}
                    </TableCell>

                    <TableCell
                      className={cn(
                        "text-right font-mono text-sm",
                        direction === "in"
                          ? "text-settled-700"
                          : "text-slate-900"
                      )}
                    >
                      {formatSignedAmount(entry)}
                    </TableCell>

                    {showRunning && (
                      <TableCell className="text-right font-mono text-sm text-slate-500">
                        {formatBalance({ amount: balanceAfter, currency })}
                      </TableCell>
                    )}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      <div className="space-y-1 text-xs text-slate-400">
        <p>{t(LEDGER_IS_APPEND_ONLY)}</p>
        {!showRunning && (
          <p>{t("payments.runningBalanceOnlyOnTheFirstPage")}</p>
        )}
      </div>

      {rows.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}
    </div>
  );
}

function BalanceCard({
  balance,
  onReconcile,
}: {
  balance: AccountBalance;
  onReconcile?: (balance: AccountBalance) => void;
}) {
  const t = useTranslation();

  const credit = isInCredit(balance);
  const debit = isInDebit(balance);
  const untouched = isUntouched(balance);

  return (
    <div
      className={cn(
        "rounded-xl border bg-card p-4",
        credit
          ? "border-settled-200 bg-settled-50"
          : debit
            ? "border-attention-200 bg-attention-50"
            : "border-slate-200"
      )}
    >
      <div className="flex items-baseline justify-between gap-3">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {toCurrencyCode(balance.currency)}
        </p>
        <span className="font-mono text-[11px] uppercase tracking-widest text-slate-500">
          {untouched
            ? t("payments.noActivity")
            : credit
              ? t("payments.inCredit")
              : debit
                ? t("payments.owed")
                : t("payments.square")}
        </span>
      </div>

      <p
        className={cn(
          "mt-1 font-mono text-2xl",
          credit
            ? "text-settled-700"
            : debit
              ? "text-attention-700"
              : "text-slate-900"
        )}
      >
        {formatBalance(balance)}
      </p>

      <p className="mt-2 text-xs text-slate-500">
        {t(balanceMeaningKey(balance))}
      </p>

      <p className="mt-1 text-[11px] text-slate-400">
        {t("payments.movementsOnThisCurrency", balance.entryCount)}
      </p>

      {onReconcile !== undefined && (
        <Button
          variant="outline"
          size="sm"
          className="mt-3 h-8 text-xs"
          onClick={() => onReconcile(balance)}
        >
          {t("reconciliation.reconcile")}
        </Button>
      )}
    </div>
  );
}
