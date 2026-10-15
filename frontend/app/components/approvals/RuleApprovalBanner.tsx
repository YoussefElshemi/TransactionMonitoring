"use client";

import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { RuleDeployment } from "@/types";
import { approvalAgeMs, approvalHref } from "@/lib/approvals";
import { durationLabel } from "@/lib/ageing";
import { versionLabel } from "@/lib/deployments";
import { relationText, shortId, userName } from "@/lib/resources";
import { useDeploymentApproval } from "@/lib/useApprovals";
import { StatusBadge } from "@/components/StatusBadge";
import { useTranslation } from "@/context/LocalizationContext";

export function RuleApprovalBanner({
  deployment,
  versionIndex,
  viewerUserId,
}: {
  deployment: RuleDeployment;
  versionIndex: Record<string, number>;
  viewerUserId?: string;
}) {
  const t = useTranslation();
  const resource = useDeploymentApproval(deployment.ruleDeploymentId);
  const approval = resource?.approval;

  const raisedBy = approval
    ? approval.requestedByUserId === viewerUserId
      ? "you"
      : relationText(
          userName(resource?.requestedByUser),
          approval.requestedByUserId
        )
    : relationText(null, deployment.createdByUserId);

  return (
    <div className="rounded-lg border border-attention-200 bg-attention-50/60 p-4">
      <div className="flex flex-wrap items-center gap-3">
        <StatusBadge status="PendingApproval" />
        <span className="text-sm text-slate-700">
          {versionLabel(deployment.previousRuleVersionId, versionIndex)}
          <span className="mx-1.5 text-slate-400">→</span>
          <span className="font-medium">
            {versionLabel(deployment.targetRuleVersionId, versionIndex)}
          </span>
          <span className="ml-2 text-slate-500">
            proposed by {raisedBy}
            {approval && ` ${durationLabel(approvalAgeMs(approval))} ago`}
          </span>
        </span>
        {approval && (
          <Link
            href={approvalHref(approval.approvalId)}
            className="ml-auto inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
          >
            {t("approvals.openTheRequest")}
            <ArrowRight className="size-3.5" />
          </Link>
        )}
      </div>

      <p className="mt-2 max-w-prose text-xs text-slate-600">
        Nothing has rolled out. The live rule keeps its current logic, and no
        traffic reaches this version, until somebody holding approvals:decide who is not the
        person who proposed it agrees. Another deployment cannot be started for
        this rule while the request is open.
      </p>

      {!approval && (
        <p className="mt-1 text-xs text-slate-500">
          The request for it could not be found in the pending queue — look for
          deployment {shortId(deployment.ruleDeploymentId)} under Approvals.
        </p>
      )}
    </div>
  );
}
