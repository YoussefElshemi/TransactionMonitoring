"use client";

import { useCallback, useEffect, useState } from "react";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams,
} from "next/navigation";
import Link from "next/link";
import { toast } from "sonner";
import { ArrowRight, Ban } from "lucide-react";

import { SuspiciousActivityReportResource } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import {
  FILINGS_HREF,
  filingPermissions,
  filingStatusText,
} from "@/lib/suspiciousActivityReports";
import { relationText, userName } from "@/lib/resources";
import { usePermissions } from "@/context/PermissionsContext";
import { FilingActions } from "@/components/suspiciousActivityReports/FilingActions";
import {
  FilingDeadline,
  FilingStatusBadge,
} from "@/components/suspiciousActivityReports/FilingParts";
import { NarrativeEditor } from "@/components/suspiciousActivityReports/NarrativeEditor";
import { HistoryPanel, historyPath } from "@/components/history";
import { LocalTime } from "@/components/LocalTime";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { TabBar } from "@/components/TabBar";
import { Field, FieldList } from "@/components/FieldList";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const POLICY_HREF = "/organization#Filing%20Policy";

const SuspiciousActivityReportDetail = () => {
  const t = useTranslation();
  const { id } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [resource, setResource] =
    useState<SuspiciousActivityReportResource | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("Details");

  const report = resource?.suspiciousActivityReport ?? null;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchReport = useCallback(
    async (reportId: string, quiet = false) => {
      if (!quiet) setLoading(true);

      try {
        const response = await apiFetch(
          `${API_BASE}/suspicious-activity-reports/${reportId}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (response.ok) {
          setResource(
            await unwrap<SuspiciousActivityReportResource>(response)
          );
        } else {
          setResource(null);
          if (response.status !== 404) {
            await reportApiError(response, {
              fallback: t("suspiciousActivityReports.errorOccurredFetchingFiling"),
              onUnauthorized: goToLogin,
              toastId: "error-fetching-filing",
            });
          }
        }
      } catch {
        setResource(null);
        toast.error(t("suspiciousActivityReports.errorOccurredFetchingFiling"), {
          id: "error-fetching-filing",
        });
      } finally {
        if (!quiet) setLoading(false);
      }
    },
    [goToLogin]
  );

  useEffect(() => {
    if (id) fetchReport(id as string);
  }, [id, fetchReport]);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-56" />
        <Card>
          <CardContent className="space-y-2 pt-6">
            {Array.from({ length: 5 }).map((_, row) => (
              <Skeleton key={row} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  if (!resource || !report) {
    return (
      <NotFound
        title={t("suspiciousActivityReports.filingNotFound")}
        description={t("suspiciousActivityReports.itMayNeverHaveExisted")}
        backHref={FILINGS_HREF}
        backLabel={t("suspiciousActivityReports.allFilings")}
      />
    );
  }

  const permissions = filingPermissions(report, {
    canWrite: holds("suspicious-activity-reports:write"),
    canFile: holds("suspicious-activity-reports:file"),
  });
  const meaning = filingStatusText(report.status);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("suspiciousActivityReports.filing")}
        title={report.friendlyName}
        description={t("suspiciousActivityReports.aSuspiciousActivityReportOpened")}
      />

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="flex flex-wrap items-center gap-x-4 gap-y-3">
            <FilingStatusBadge status={report.status} />
            <FilingDeadline report={report} />
            <Link
              href={`/cases/${report.caseId}`}
              className="ml-auto inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
            >
              {t("suspiciousActivityReports.openTheCase")}
              <ArrowRight className="size-3.5" />
            </Link>
          </div>

          {meaning && (
            <p className="max-w-prose text-sm text-slate-600">{meaning}</p>
          )}
        </CardContent>
      </Card>

      {report.status === "NotFiled" && (
        <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
          <div className="flex flex-wrap items-center gap-3">
            <span className="inline-flex items-center gap-1.5 font-mono text-xs uppercase tracking-widest text-slate-500">
              <Ban className="size-4 text-slate-400" />
              {t("suspiciousActivityReports.consideredAndNotFiled")}
            </span>
            <span className="text-sm text-slate-600">
              Decided by{" "}
              <span className="font-medium text-slate-900">
                {relationText(
                  userName(resource.decidedByUser),
                  report.decidedByUserId
                )}
              </span>
              {report.decidedAt && (
                <>
                  {" · "}
                  <LocalTime
                    value={report.decidedAt}
                    className="font-mono text-xs text-slate-500"
                  />
                </>
              )}
            </span>
          </div>
          <p className="mt-3 whitespace-pre-wrap text-sm text-slate-700">
            {report.notFiledReason}
          </p>
          <p className="mt-3 max-w-prose text-xs text-slate-500">
            This is not the same as a case nobody looked at. Somebody weighed
            this one and concluded it does not need reporting, and that
            conclusion is now part of the permanent record.
          </p>
        </div>
      )}

      <div className="space-y-4">
        <TabBar
          tabs={["Details", "History"]}
          activeTab={activeTab}
          onChange={setActiveTab}
        />

        {activeTab === "Details" ? (
          <div className="space-y-6">
            <NarrativeEditor
              report={report}
              onUpdated={setResource}
              onConflict={() =>
                fetchReport(report.suspiciousActivityReportId, true)
              }
              onUnauthorized={goToLogin}
            />

            <FilingActions
              report={report}
              requireFilingApproval={resource.requireFilingApproval}
              permissions={permissions}
              onUpdated={setResource}
              onConflict={() =>
                fetchReport(report.suspiciousActivityReportId, true)
              }
              onUnauthorized={goToLogin}
              policyHref={POLICY_HREF}
              canManagePolicy={holds(
                "suspicious-activity-report-policies:write"
              )}
            />

            <Card>
              <CardContent className="p-0">
                <FieldList>
                  <Field label={t("common.case")}>
                    <Link
                      href={`/cases/${report.caseId}`}
                      className="font-mono text-xs text-brand-800 hover:underline"
                    >
                      {report.caseId}
                    </Link>
                  </Field>
                  <Field label={t("suspiciousActivityReports.openedBy")}>
                    {relationText(
                      userName(resource.openedByUser),
                      report.openedByUserId
                    )}
                  </Field>
                  <Field label={t("suspiciousActivityReports.opened")} mono>
                    <LocalTime value={report.createdAt} />
                  </Field>
                  <Field label={t("suspiciousActivityReports.deadline")} mono>
                    <LocalTime value={report.dueAt} />
                  </Field>

                  {report.filingReference && (
                    <Field label={t("suspiciousActivityReports.filingReference")} mono>
                      <span className="break-all">
                        {report.filingReference}
                      </span>
                    </Field>
                  )}
                  {report.filedByUserId && (
                    <Field label={t("suspiciousActivityReports.filedBy")}>
                      {relationText(
                        userName(resource.filedByUser),
                        report.filedByUserId
                      )}
                    </Field>
                  )}
                  {report.filedAt && (
                    <Field label={t("suspiciousActivityReports.filed")} mono>
                      <LocalTime value={report.filedAt} />
                    </Field>
                  )}
                  {report.acknowledgementReference && (
                    <Field label={t("suspiciousActivityReports.acknowledgementReference")} mono>
                      <span className="break-all">
                        {report.acknowledgementReference}
                      </span>
                    </Field>
                  )}
                  {report.acknowledgedAt && (
                    <Field label={t("suspiciousActivityReports.acknowledged")} mono>
                      <LocalTime value={report.acknowledgedAt} />
                    </Field>
                  )}
                  {report.notFiledReason && (
                    <Field label={t("suspiciousActivityReports.notFiledBecause")}>
                      <span className="whitespace-pre-wrap">
                        {report.notFiledReason}
                      </span>
                    </Field>
                  )}
                  {report.decidedByUserId && (
                    <Field label={t("suspiciousActivityReports.decidedBy")}>
                      {relationText(
                        userName(resource.decidedByUser),
                        report.decidedByUserId
                      )}
                    </Field>
                  )}
                  {report.decidedAt && (
                    <Field label={t("suspiciousActivityReports.decided")} mono>
                      <LocalTime value={report.decidedAt} />
                    </Field>
                  )}

                  <Field label={t("suspiciousActivityReports.filingId")} mono>
                    <span className="break-all text-xs text-slate-500">
                      {report.suspiciousActivityReportId}
                    </span>
                  </Field>
                </FieldList>
              </CardContent>
            </Card>
          </div>
        ) : (
          <HistoryPanel
            entityType="SuspiciousActivityReport"
            path={historyPath.SuspiciousActivityReport(
              report.suspiciousActivityReportId
            )}
            entityId={report.suspiciousActivityReportId}
            onUnauthorized={goToLogin}
            scopeNote={
              "Every move this filing made and who made it — including each version of the narrative, since the text can only be rewritten while it is a draft. Nothing on a filing is ever deleted, so nothing is missing from this."
            }
          />
        )}
      </div>
    </div>
  );
};

export default SuspiciousActivityReportDetail;
