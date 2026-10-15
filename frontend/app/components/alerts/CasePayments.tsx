"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Gavel } from "lucide-react";

import { AlertResource, Decision, TransactionResource } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { isTerminalDecision } from "@/lib/decisions";
import { rulesById, usersById } from "@/lib/resources";
import { DecisionBadge } from "@/components/DecisionBadge";
import { DecisionPanel } from "@/components/DecisionPanel";
import { HoldCountdown } from "@/components/HoldExpiry";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { CaseClose, CloseCaseDialog } from "@/components/cases/CloseCaseDialog";

import { apiFetch } from "@/lib/http";
import { mapBounded } from "@/lib/rateLimit";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { useTranslation } from "@/context/LocalizationContext";

interface Payment {
  resource: TransactionResource;
  decision: Decision | null;
}

const ALERT_PAGE_SIZE = 100;

export function CasePayments({
  caseId,
  originatingTransactionId,
  organizationId,
  onResolved,
  onUnauthorized,
}: {
  caseId: string;
  originatingTransactionId: string | null;
  organizationId: string;
  onResolved: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalAlerts, setTotalAlerts] = useState(0);
  const [truncated, setTruncated] = useState(false);
  const [resolving, setResolving] = useState<string | null>(null);
  const settlement = useIdempotencyKey();
  const [settling, setSettling] = useState<Payment | null>(null);

  const fetchPayments = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/alerts?PageNumber=1&PageSize=${ALERT_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (!response.ok) {
        setPayments([]);
        await reportApiError(response, {
          fallback: t("alerts.errorOccurredFetchingCaseS2"),
          onUnauthorized,
          toastId: "error-fetching-case-payments",
        });
        return;
      }

      const page = await unwrapPage<AlertResource>(response);
      setTotalAlerts(page.metadata.totalCount);
      setTruncated(page.metadata.hasMore);

      const transactionIds = [
        ...new Set(
          [
            ...page.data.map(({ alert }) => alert.transactionId),
            ...(originatingTransactionId ? [originatingTransactionId] : []),
          ]
        ),
      ];

      const resources = await mapBounded(
        transactionIds,
        async (transactionId) => {
          try {
            const transactionResponse = await apiFetch(
              `${API_BASE}/transactions/${transactionId}`,
              {
                method: "GET",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
              }
            );

            return transactionResponse.ok
              ? await unwrap<TransactionResource>(transactionResponse)
              : null;
          } catch {
            return null;
          }
        }
      );

      setPayments(
        resources
          .filter((resource): resource is TransactionResource => resource !== null)
          .map((resource) => ({
            resource,
            decision: resource.transaction.decision,
          }))
          .sort((left, right) => outstandingRank(right) - outstandingRank(left))
      );
    } catch {
      setPayments([]);
      toast.error(t("alerts.errorOccurredFetchingCaseS2"), {
        id: "error-fetching-case-payments",
      });
    } finally {
      setLoading(false);
    }
  }, [caseId, originatingTransactionId, onUnauthorized]);

  useEffect(() => {
    fetchPayments();
  }, [fetchPayments]);

  const closesCase = (decisionId: string): boolean | null => {
    if (truncated) return null;

    return payments.every(
      ({ decision }) =>
        !decision ||
        decision.decisionId === decisionId ||
        isTerminalDecision(decision.status)
    );
  };

  const resolveDecision = async (decisionId: string, close: CaseClose) => {
    setResolving(decisionId);

    try {
      const response = await apiFetch(
        `${API_BASE}/decisions/${decisionId}/resolve`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(close),
        },
        { idempotencyKey: settlement.current(decisionId) }
      );

      if (response.ok) {
        settlement.completed(decisionId);
        toast.success(
          close.status === "Approved" ? t("alerts.paymentReleased") : t("alerts.paymentBlocked")
        );
        setSettling(null);
        await fetchPayments();
        onResolved();
      } else {
        await reportApiError(response, {
          fallback: t("alerts.errorOccurredResolvingPayment"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("alerts.errorOccurredResolvingPayment"));
    } finally {
      setResolving(null);
    }
  };

  const held = payments.filter(
    (payment) => payment.decision?.status === "Held"
  );

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">{t("alerts.transactions")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  if (payments.length === 1) {
    const [only] = payments;

    return (
      <DecisionPanel
        decision={only.decision}
        showCaseLink={false}
        transactionHref={`/transactions/${only.resource.transaction.transactionId}`}
        showFiredRules={false}
        showHold
        rules={rulesById(only.resource.rules)}
        users={usersById(only.resource.users)}
      />
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center gap-3 text-base font-semibold">
          {t("alerts.transactions")}
          <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            {payments.length} under this case
          </span>
          {held.length > 0 && (
            <span className="font-mono text-[11px] uppercase tracking-widest text-attention-700">
              {held.length} still held
            </span>
          )}
        </CardTitle>
      </CardHeader>

      <CardContent className="space-y-3 text-sm">
        <p className="text-slate-500">
          Every payment this case gathered, each with its own verdict. Settling
          one here answers that payment only — resolving the case, from the
          buttons at the top of this page, answers all of them together. The
          last one you settle closes the case, and asks for the conclusion and
          reason code a close carries.
        </p>

        {truncated && (
          <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
            This case has {totalAlerts} alerts, more than one page. The payments
            below are the ones named by the first {ALERT_PAGE_SIZE} of them, so
            treat the count as a floor rather than the whole case — and settling
            the last one shown may still not be the settle that closes it, in
            which case the server will say so.
          </p>
        )}

        {payments.length === 0 ? (
          <p className="text-slate-400">
            {t("alerts.noTransactionsCouldBeRead")}
          </p>
        ) : (
          <ul className="space-y-2">
            {payments.map(({ resource, decision }) => (
              <li
                key={resource.transaction.transactionId}
                className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-slate-200 bg-card px-3 py-2"
              >
                <div className="flex min-w-0 flex-col gap-1">
                  <Link
                    href={`/transactions/${resource.transaction.transactionId}`}
                    className="truncate font-mono text-sm text-brand-800 hover:underline"
                  >
                    {resource.transaction.friendlyName}
                  </Link>
                  {resource.transaction.externalReference && (
                    <span className="truncate font-mono text-xs text-slate-400">
                      {resource.transaction.externalReference}
                    </span>
                  )}
                </div>

                <div className="flex flex-shrink-0 flex-wrap items-center gap-2">
                  {decision ? (
                    <DecisionBadge
                      status={decision.status}
                      reasonCode={decision.reasonCode}
                    />
                  ) : (
                    <span className="font-mono text-[11px] text-slate-400">
                      no verdict recorded
                    </span>
                  )}

                  {decision?.status === "Held" && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5 text-inflight-700 hover:text-inflight-700"
                      disabled={resolving === decision.decisionId}
                      onClick={() => setSettling({ resource, decision })}
                    >
                      <Gavel className="size-4" />
                      {t("alerts.settle")}
                    </Button>
                  )}
                </div>

                {decision?.status === "Held" && (
                  <div className="w-full">
                    <HoldCountdown decision={decision} />
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </CardContent>

      {settling?.decision && (
        <CloseCaseDialog
          open
          onOpenChange={(open) => !open && setSettling(null)}
          organizationId={organizationId}
          submitting={resolving === settling.decision.decisionId}
          subject="payment"
          concludes={closesCase(settling.decision.decisionId) ?? false}
          contextLabel={settling.resource.transaction.friendlyName}
          onSubmit={(close) =>
            resolveDecision(settling.decision!.decisionId, close)
          }
        />
      )}
    </Card>
  );
}

const outstandingRank = (payment: Payment): number => {
  if (!payment.decision) return 0;
  if (payment.decision.status === "Held") return 2;
  return isTerminalDecision(payment.decision.status) ? 0 : 1;
};
