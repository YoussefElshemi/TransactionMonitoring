"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { ArrowRight } from "lucide-react";

import { TransactionResource } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  relationText,
  rulesById,
  userName,
  usersById,
} from "@/lib/resources";
import { loginHref } from "@/lib/authRedirect";
import { usePermissions } from "@/context/PermissionsContext";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { RevealablePayload } from "@/components/RevealablePayload";
import { DecisionBadge } from "@/components/DecisionBadge";
import { DecisionPanel } from "@/components/DecisionPanel";
import { RuleEvidence } from "@/components/evidence";
import { TransactionParties } from "@/components/parties/TransactionParties";
import { StatusBadge } from "@/components/StatusBadge";
import { TabBar } from "@/components/TabBar";
import {
  HistoryPanel,
  historyPath,
  TRANSACTION_HISTORY_SCOPE_NOTE,
} from "@/components/history";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const TransactionDetails = () => {
  const t = useTranslation();
  const { transactionId } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds, holdsAll } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [resource, setResource] = useState<TransactionResource | null>(null);
  const transaction = resource?.transaction ?? null;
  const rules = useMemo(() => rulesById(resource?.rules), [resource]);
  const users = useMemo(() => usersById(resource?.users), [resource]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("Evaluations");

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  useEffect(() => {
    const fetchTransaction = async () => {
      setLoading(true);

      try {
        const response = await apiFetch(
          `${API_BASE}/transactions/${transactionId}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (response.ok) {
          setResource(await unwrap<TransactionResource>(response));
        } else if (response.status === 404) {
          setResource(null);
        } else {
          setResource(null);
          await reportApiError(response, {
            fallback: t("transactions.errorOccurredFetchingTransaction"),
            onUnauthorized: goToLogin,
            toastId: "error-fetching-transaction",
          });
        }
      } catch {
        setResource(null);
        toast.error(t("transactions.errorOccurredFetchingTransaction"), {
          id: "error-fetching-transaction",
        });
      } finally {
        setLoading(false);
      }
    };

    fetchTransaction();
  }, [transactionId, goToLogin]);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Card>
          <CardContent className="space-y-2 pt-6">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!transaction) {
    return (
      <NotFound
        title={t("transactions.transactionNotFound")}
        description={t("transactions.noTransactionWithThatId")}
        backHref="/transactions"
        backLabel={t("transactions.allTransactions")}
      />
    );
  }

  const tabs = ["Evaluations", "Cases"];
  if (holdsAll("transactions:read", "audit-logs:read")) tabs.push("History");

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("transactions.transaction")}
        title={transaction.externalReference ?? transaction.friendlyName}
      >
        {transaction.decision && (
          <DecisionBadge
            status={transaction.decision.status}
            reasonCode={transaction.decision.reasonCode}
          />
        )}
      </PageHeader>

      <DecisionPanel
        decision={transaction.decision}
        rules={rules}
        users={users}
      />

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">{t("common.details")}</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("transactions.yourReference")}</span>
            {transaction.externalReference ? (
              <p className="mt-0.5 break-all font-mono text-slate-900">
                {transaction.externalReference}
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                {t("transactions.noneThisTransactionWasEvaluated")}
              </p>
            )}
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("transactions.platformName")}</span>
            <p className="mt-0.5 font-mono text-slate-900">
              {transaction.friendlyName}
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.created")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={transaction.createdAt} />
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.updated")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={transaction.updatedAt} />
            </p>
          </div>
        </CardContent>
      </Card>

      <TransactionParties
        parties={transaction.parties ?? []}
        canManage={holds("party-mappings:write")}
      />

      <div>
        <h2 className="mb-3 font-mono text-sm font-medium tracking-tight text-slate-900">
          {t("transactions.payload")}
        </h2>
        <RevealablePayload
          transactionId={transaction.transactionId}
          payload={transaction.payload}
          masked={transaction.payloadMasked}
          onUnauthorized={goToLogin}
        />
      </div>

      <div className="space-y-4">
        <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

        {activeTab === "Evaluations" && (
          <div className="space-y-2">
            {transaction.ruleEvaluations.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-500">
                {t("transactions.noRuleHasEvaluatedThis")}
              </p>
            ) : (
              transaction.ruleEvaluations.map((evaluation) => (
                <RuleEvidence
                  key={evaluation.ruleEvaluationId}
                  evaluation={evaluation}
                  payload={transaction.payload}
                  rule={rules.get(evaluation.ruleId)}
                />
              ))
            )}
          </div>
        )}

        {activeTab === "Cases" && (
          <div className="space-y-2">
            {transaction.cases.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-500">
                {t("transactions.noCaseHasBeenOpened")}
              </p>
            ) : (
              transaction.cases.map((_case) => (
                <Link
                  key={_case.caseId}
                  href={`/cases/${_case.caseId}`}
                  className="flex items-center justify-between gap-3 rounded-md border border-slate-200 bg-card p-4 transition-colors hover:border-slate-300 hover:bg-slate-50/50"
                >
                  <div className="min-w-0">
                    <span className="text-sm font-medium text-slate-900">
                      {_case.friendlyName}
                    </span>
                    <p className="mt-0.5 text-xs text-slate-400">
                      <LocalTime value={_case.createdAt} />
                      {_case.assignedToUserId
                        ? ` · ${relationText(
                            userName(users.get(_case.assignedToUserId)),
                            _case.assignedToUserId
                          )}`
                        : ""}
                    </p>
                  </div>
                  <div className="flex flex-shrink-0 items-center gap-3">
                    <StatusBadge status={_case.status} />
                    <ArrowRight className="size-4 text-slate-300" />
                  </div>
                </Link>
              ))
            )}
          </div>
        )}

        {activeTab === "History" && (
          <HistoryPanel
            entityType="Transaction"
            path={historyPath.Transaction(transaction.transactionId)}
            entityId={transaction.transactionId}
            onUnauthorized={goToLogin}
            scopeNote={TRANSACTION_HISTORY_SCOPE_NOTE}
          />
        )}
      </div>
    </div>
  );
};

export default TransactionDetails;
