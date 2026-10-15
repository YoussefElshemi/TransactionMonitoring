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
import { ArrowRight } from "lucide-react";

import { ApprovalResource } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import {
  approvalOutcomeTextKey,
  approvalPermissions,
  approvalSubjectLabel,
  isPendingApproval,
} from "@/lib/approvals";
import { useReasonCodes } from "@/lib/reasonCodes";
import { relationText, userName } from "@/lib/resources";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { ApprovalActions } from "@/components/approvals/ApprovalActions";
import {
  ApprovalAge,
  ApprovalOutcomeNote,
  ApprovalStatusBadge,
  ProposedResolutionPanel,
} from "@/components/approvals/ApprovalParts";
import { DeploymentProposal } from "@/components/approvals/DeploymentProposal";
import { FilingProposal } from "@/components/approvals/FilingProposal";
import { HistoryPanel, historyPath } from "@/components/history";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { TabBar } from "@/components/TabBar";
import { Field, FieldList } from "@/components/FieldList";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const ApprovalDetail = () => {
  const t = useTranslation();
  const { approvalId } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const { holds } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [resource, setResource] = useState<ApprovalResource | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("Details");

  const approval = resource?.approval ?? null;

  const { codes } = useReasonCodes(
    approval?.proposedCaseResolution ? user?.organizationId : null
  );

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchApproval = useCallback(
    async (id: string, quiet = false) => {
      if (!quiet) setLoading(true);

      try {
        const response = await apiFetch(`${API_BASE}/approvals/${id}`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });

        if (response.ok) {
          setResource(await unwrap<ApprovalResource>(response));
        } else {
          setResource(null);
          if (response.status !== 404) {
            await reportApiError(response, {
              fallback: t("approvals.errorOccurredFetchingRequest"),
              onUnauthorized: goToLogin,
              toastId: "error-fetching-approval",
            });
          }
        }
      } catch {
        setResource(null);
        toast.error(t("approvals.errorOccurredFetchingRequest"), {
          id: "error-fetching-approval",
        });
      } finally {
        if (!quiet) setLoading(false);
      }
    },
    [goToLogin]
  );

  useEffect(() => {
    if (approvalId) fetchApproval(approvalId as string);
  }, [approvalId, fetchApproval]);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-56" />
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

  if (!resource || !approval) {
    return (
      <NotFound
        title={t("approvals.requestNotFound")}
        description={t("approvals.itMayNeverHaveExisted")}
        backHref="/approvals"
        backLabel={t("approvals.allApprovals")}
      />
    );
  }

  const permissions = approvalPermissions(
    approval,
    user,
    holds("approvals:decide")
  );
  const pending = isPendingApproval(approval);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("approvals.fourEyes")}
        title={approvalSubjectLabel(approval.subject)}
        description={
          pending
            ? t(approvalOutcomeTextKey(approval.subject))
            : t("approvals.settledRequestKeptAnsweredRecord")
        }
      />

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div className="flex flex-wrap items-center gap-3">
            <ApprovalStatusBadge status={approval.status} />
            <ApprovalAge approval={approval} />
          </div>

          {pending ? (
            <ApprovalActions
              resource={resource}
              permissions={permissions}
              onSettled={setResource}
              onConflict={() => fetchApproval(approval.approvalId, true)}
              onUnauthorized={goToLogin}
            />
          ) : (
            <>
              <ApprovalOutcomeNote resource={resource} />
              {approval.status === "Rejected" &&
                approval.subject === "SuspiciousActivityReport" && (
                  <p className="max-w-prose text-sm text-slate-600">
                    This report went back to draft. It is not finished with —
                    the narrative can be rewritten and submitted again, and its
                    deadline has been running throughout.
                  </p>
                )}
            </>
          )}
        </CardContent>
      </Card>

      <div className="space-y-4">
        <TabBar
          tabs={["Details", "History"]}
          activeTab={activeTab}
          onChange={setActiveTab}
        />

        {activeTab === "Details" && (
          <div className="space-y-6">
            <section className="space-y-2">
              <h2 className="font-mono text-sm font-medium tracking-tight text-slate-900">
                {t("approvals.whatIsBeingAsked")}
              </h2>

              {approval.subject === "RuleDeployment" && (
                <DeploymentProposal resource={resource} />
              )}

              {approval.subject === "SuspiciousActivityReport" && (
                <FilingProposal resource={resource} />
              )}

              {approval.subject === "CaseResolution" && (
                <>
                  <ProposedResolutionPanel
                    approval={approval}
                    reasonCodes={codes}
                  />
                  {approval.caseId && (
                    <Link
                      href={`/cases/${approval.caseId}`}
                      className="inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
                    >
                      {t("approvals.openTheCase")}
                      <ArrowRight className="size-3.5" />
                    </Link>
                  )}
                  {pending && (
                    <p className="max-w-prose text-xs text-slate-500">
                      The case is still open and still holding its payments.
                      Nothing above has been applied to it.
                    </p>
                  )}
                </>
              )}
            </section>

            <Card>
              <CardContent className="p-0">
                <FieldList>
                  <Field label={t("approvals.raisedBy")}>
                    {relationText(
                      userName(resource.requestedByUser),
                      approval.requestedByUserId
                    )}
                  </Field>
                  <Field label={t("approvals.raised")} mono>
                    <LocalTime value={approval.createdAt} />
                  </Field>
                  {approval.reason && (
                    <Field label={t("approvals.reasonGiven")}>
                      <span className="whitespace-pre-wrap">
                        {approval.reason}
                      </span>
                    </Field>
                  )}
                  <Field label={t("approvals.reviewedBy")}>
                    {approval.reviewedByUserId ? (
                      relationText(
                        userName(resource.reviewedByUser),
                        approval.reviewedByUserId
                      )
                    ) : (
                      <span className="text-slate-400">
                        {pending ? "Nobody yet" : "Nobody"}
                      </span>
                    )}
                  </Field>
                  <Field label={t("approvals.reviewed")} mono>
                    {approval.reviewedAt ? (
                      <LocalTime value={approval.reviewedAt} />
                    ) : (
                      <span className="font-sans text-slate-400">—</span>
                    )}
                  </Field>
                  <Field label={t("approvals.requestId")} mono>
                    <span className="break-all text-xs text-slate-500">
                      {approval.approvalId}
                    </span>
                  </Field>
                </FieldList>
              </CardContent>
            </Card>
          </div>
        )}

        {activeTab === "History" && (
          <HistoryPanel
            entityType="Approval"
            path={historyPath.Approval(approval.approvalId)}
            entityId={approval.approvalId}
            onUnauthorized={goToLogin}
            scopeNote={
              "Only the review is tracked here — the status, who answered it, when, and their note. Everything else about a request is fixed when it is raised."
            }
          />
        )}
      </div>
    </div>
  );
};

export default ApprovalDetail;
