"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { toast } from "sonner";
import { ArrowRight, Layers } from "lucide-react";

import {
  AlertResource,
  Decision,
  Rule,
  TransactionResource,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { alertStatusText } from "@/lib/alerts";
import { partyName } from "@/lib/parties";
import { relationText, rulesById, shortId, usersById } from "@/lib/resources";
import { useParty } from "@/lib/useParties";
import {
  AlertStatusBadge,
  RaisedAt,
  SuppressionNote,
} from "@/components/alerts/AlertParts";
import { RoleChip } from "@/components/parties/PartyParts";
import { DecisionPanel } from "@/components/DecisionPanel";
import { PageHeader } from "@/components/PageHeader";
import { PayloadView } from "@/components/PayloadView";
import { NotFound } from "@/components/NotFound";
import { SeverityBadge } from "@/components/SeverityBadge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const AlertDetail = () => {
  const t = useTranslation();
  const { alertId } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [resource, setResource] = useState<AlertResource | null>(null);
  const [loading, setLoading] = useState(true);

  const alert = resource?.alert ?? null;

  const party = useParty(alert?.partyId ?? null);

  const [transactionResource, setTransactionResource] =
    useState<TransactionResource | null>(null);
  const [decisionLoading, setDecisionLoading] = useState(false);
  const decision: Decision | null =
    transactionResource?.transaction.decision ?? null;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchAlert = useCallback(
    async (id: string) => {
      setLoading(true);

      try {
        const response = await apiFetch(`${API_BASE}/alerts/${id}`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });

        if (response.ok) {
          setResource(await unwrap<AlertResource>(response));
        } else {
          setResource(null);
          if (response.status !== 404) {
            await reportApiError(response, {
              fallback: t("alerts.errorOccurredFetchingAlert"),
              onUnauthorized: goToLogin,
              toastId: "error-fetching-alert",
            });
          }
        }
      } catch {
        setResource(null);
        toast.error(t("alerts.errorOccurredFetchingAlert"), {
          id: "error-fetching-alert",
        });
      } finally {
        setLoading(false);
      }
    },
    [goToLogin]
  );

  const fetchDecision = useCallback(async (transactionId: string) => {
    setDecisionLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/transactions/${transactionId}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      setTransactionResource(
        response.ok ? await unwrap<TransactionResource>(response) : null
      );
    } catch {
      setTransactionResource(null);
    } finally {
      setDecisionLoading(false);
    }
  }, []);

  useEffect(() => {
    if (alertId) fetchAlert(alertId as string);
  }, [alertId, fetchAlert]);

  useEffect(() => {
    if (alert?.transactionId) fetchDecision(alert.transactionId);
    else setTransactionResource(null);
  }, [alert?.transactionId, fetchDecision]);

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

  if (!alert) {
    return (
      <NotFound
        title={t("alerts.alertNotFound")}
        description={t("common.notFound.description")}
        backHref="/alerts"
        backLabel={t("alerts.allAlerts")}
      />
    );
  }

  const rule: Rule | undefined = resource?.rule;
  const transaction = resource?.transaction;
  const statusMeaning = alertStatusText(alert.status);

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("alerts.alert")} title={alert.friendlyName} />

      <SuppressionNote alert={alert} />

      <Card>
        <CardHeader>
          <CardTitle className="flex flex-wrap items-center gap-3 text-base font-semibold">
            {t("common.details")}
            <AlertStatusBadge status={alert.status} />
            <SeverityBadge severity={alert.severity} emphasis />
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
          {statusMeaning && (
            <p className="col-span-2 text-slate-600">{statusMeaning}</p>
          )}

          <div>
            <span className="font-medium text-slate-500">{t("alerts.raised")}</span>
            <p className="mt-0.5">
              <RaisedAt alert={alert} />
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("alerts.recorded")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={alert.createdAt} />
            </p>
          </div>

          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("common.rule")}</span>
            <p className="mt-0.5">
              {rule?.isDeleted ? (
                <span className="text-slate-400">
                  {relationText(rule.friendlyName, alert.ruleId)} (Deleted)
                </span>
              ) : (
                <Link
                  href={`/rules/${alert.ruleId}`}
                  className="text-brand-800 hover:underline"
                >
                  {relationText(rule?.friendlyName, alert.ruleId)}
                </Link>
              )}
            </p>
            <p
              className="mt-0.5 break-all font-mono text-xs text-slate-500"
              title={t("alerts.theExactRuleVersionThat")}
            >
              version {shortId(alert.ruleVersionId)}
            </p>
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("alerts.evaluation")}</span>
            <p className="mt-0.5 break-all font-mono text-xs text-slate-500">
              {alert.ruleEvaluationId}
            </p>
          </div>

          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("alerts.subject")}</span>
            {alert.partyId ? (
              <div className="mt-0.5 flex flex-wrap items-center gap-2">
                <Link
                  href={`/parties/${alert.partyId}`}
                  className="text-brand-800 hover:underline"
                >
                  {party ? partyName(party) : shortId(alert.partyId)}
                </Link>
                {alert.partyRole && <RoleChip role={alert.partyRole} muted />}
              </div>
            ) : (
              <p className="mt-0.5 text-slate-500">
                No subject — this organisation resolves no parties, so this
                alert stands one-to-one with its case.
              </p>
            )}
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("alerts.transaction")}</span>
            <p className="mt-0.5">
              <Link
                href={`/transactions/${alert.transactionId}`}
                className="font-mono text-brand-800 hover:underline"
              >
                {transaction?.friendlyName ?? shortId(alert.transactionId)}
              </Link>
            </p>
            {transaction?.externalReference && (
              <p className="mt-0.5 break-all font-mono text-xs text-slate-400">
                {transaction.externalReference}
              </p>
            )}
          </div>

          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("common.case")}</span>
            {alert.caseId ? (
              <p className="mt-0.5">
                <Link
                  href={`/cases/${alert.caseId}`}
                  className="text-brand-800 hover:underline"
                >
                  {t("alerts.theCaseThisWasFiled")}
                </Link>
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                {t("alerts.noneThisAlertOpenedNo")}
              </p>
            )}
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("alerts.closed")}</span>
            {alert.closedAt ? (
              <p className="mt-0.5 text-slate-900">
                <LocalTime value={alert.closedAt} />
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                {alert.status === "Closed"
                  ? "Not recorded"
                  : "Still open — closes when its case resolves"}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      {decisionLoading && !decision ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <DecisionPanel
          decision={decision}
          transactionHref={`/transactions/${alert.transactionId}`}
          rules={rulesById(transactionResource?.rules)}
          users={usersById(transactionResource?.users)}
        />
      )}

      {alert.partyId && (
        <div className="flex flex-wrap items-center gap-4">
          <Link
            href={`/alerts?partyId=${alert.partyId}&status=All`}
            className="inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
          >
            <Layers className="size-3.5" />
            {t("alerts.everyAlertAboutThisParty")}
          </Link>
          <Link
            href={`/alerts?ruleId=${alert.ruleId}&partyId=${alert.partyId}&status=All`}
            className="inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
          >
            {t("alerts.thisRuleAposSAlerts")}
            <ArrowRight className="size-3.5" />
          </Link>
        </div>
      )}

      {transaction?.payload && (
        <div>
          <h2 className="mb-3 font-mono text-sm font-medium tracking-tight text-slate-900">
            {t("alerts.payload")}
          </h2>
          <PayloadView
            payload={transaction.payload}
            masked={transaction.payloadMasked}
          />
        </div>
      )}
    </div>
  );
};

export default AlertDetail;
