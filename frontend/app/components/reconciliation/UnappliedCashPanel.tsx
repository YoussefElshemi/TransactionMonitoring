"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Coins } from "lucide-react";

import type { AccountBalance } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { toCurrencyCode } from "@/lib/currency";
import { UNAPPLIED_CASH_EXPLAINED, formatBalance } from "@/lib/account";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useOrganizationNames } from "@/lib/useOrganizationNames";
import Pagination from "@/components/Pagination";
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
import { ReconcileDialog } from "@/components/reconciliation/ReconcileDialog";
import { useTranslation } from "@/context/LocalizationContext";

export function UnappliedCashPanel({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants, resolved } = usePermissions();

  const mayRead = canWriteBilling(grants, "payments:reconcile");
  const mayRefund = canWriteBilling(grants, "payments:refund");

  const [balances, setBalances] = useState<AccountBalance[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [opened, setOpened] = useState<AccountBalance | null>(null);

  const organizationName = useOrganizationNames(
    [...new Set(balances.map((balance) => balance.organizationId))]
  );

  const fetchUnapplied = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/reconciliation/unapplied?PageNumber=${pageNumber}&PageSize=25`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<AccountBalance>(response);
        setBalances(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("reconciliation.unappliedCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-unapplied",
        });
      }
    } catch {
      toast.error(t("reconciliation.unappliedCouldNotBeRead"), {
        id: "error-fetching-unapplied",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchUnapplied();
    else setLoading(false);
  }, [mayRead, fetchUnapplied]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")}{" "}
          <span className="font-mono text-xs">payments:reconcile</span>{" "}
          {t("reconciliation.and")}{" "}
          <span className="font-mono text-xs">platform:administer</span>.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-start gap-2">
        <Coins className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("reconciliation.unappliedCash")}
          </p>
          <p className="mt-1 max-w-2xl text-sm text-slate-500">
            {t(UNAPPLIED_CASH_EXPLAINED)}
          </p>
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("reconciliation.customer")}</TableHead>
              <TableHead className="w-24">{t("common.currency")}</TableHead>
              <TableHead className="w-36 text-right">
                {t("reconciliation.onAccount")}
              </TableHead>
              <TableHead className="w-32 text-right">
                {t("reconciliation.movements")}
              </TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 5 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : balances.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("reconciliation.nothingUnapplied")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {t("reconciliation.everyPaymentHasBeenApplied")}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              balances.map((balance) => (
                <TableRow
                  key={`${balance.organizationId}-${balance.currency}`}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="text-sm">
                    <Link
                      href={`/admin/organizations/${balance.organizationId}`}
                      className="text-slate-900 underline-offset-2 hover:underline"
                    >
                      {organizationName(balance.organizationId) ??
                        balance.organizationId.slice(0, 8)}
                    </Link>
                  </TableCell>

                  <TableCell className="font-mono text-xs text-slate-500">
                    {toCurrencyCode(balance.currency)}
                  </TableCell>

                  <TableCell className="text-right font-mono text-sm text-settled-700">
                    {formatBalance(balance)}
                  </TableCell>

                  <TableCell className="text-right font-mono text-xs text-slate-500">
                    {balance.entryCount}
                  </TableCell>

                  <TableCell>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 text-xs"
                      onClick={() => setOpened(balance)}
                    >
                      {t("reconciliation.reconcile")}
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {balances.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <ReconcileDialog
        balance={opened}
        mayRefund={mayRefund}
        organizationName={
          opened === null ? null : organizationName(opened.organizationId)
        }
        onClose={() => setOpened(null)}
        onChanged={fetchUnapplied}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
