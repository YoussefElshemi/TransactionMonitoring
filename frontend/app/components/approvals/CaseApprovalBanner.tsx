"use client";

import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { ApprovalResource } from "@/types";
import { approvalAgeMs, approvalHref } from "@/lib/approvals";
import { durationLabel } from "@/lib/ageing";
import { relationText, shortId, userName } from "@/lib/resources";
import { ProposedResolutionSummary } from "@/components/approvals/ApprovalParts";
import { StatusBadge } from "@/components/StatusBadge";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function CaseApprovalBanner({
  resource,
  viewerUserId,
}: {
  resource: ApprovalResource;
  viewerUserId?: string;
}) {
  const t = useTranslation();
  const format = useFormat();
  const { approval, requestedByUser } = resource;
  const raisedBy =
    approval.requestedByUserId === viewerUserId
      ? "you"
      : relationText(userName(requestedByUser), approval.requestedByUserId);

  return (
    <div className="rounded-lg border border-attention-200 bg-attention-50/60 p-4">
      <div className="flex flex-wrap items-center gap-3">
        <StatusBadge status="Pending" label={t("approvals.awaitingApproval")} />
        <span className="text-sm text-slate-700">
          {t("approvals.raisedBy")} <span className="font-medium">{raisedBy}</span>{" "}
          {durationLabel(approvalAgeMs(approval))} ago
          <span
            className="ml-1.5 text-slate-500"
            title={format.instant(approval.createdAt)}
          >
            ({format.instant(approval.createdAt, "date")})
          </span>
        </span>
        <Link
          href={approvalHref(approval.approvalId)}
          className="ml-auto inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
        >
          {t("approvals.openTheRequest")}
          <ArrowRight className="size-3.5" />
        </Link>
      </div>

      {approval.proposedCaseResolution && (
        <div className="mt-3 flex flex-wrap items-center gap-2 text-sm text-slate-600">
          <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("approvals.proposed")}
          </span>
          <ProposedResolutionSummary approval={approval} />
        </div>
      )}

      <p className="mt-2 max-w-prose text-xs text-slate-600">
        Nothing above has been applied. This case is still open and still holding
        its payments until somebody other than the requester answers the
        request, and it cannot be resolved again in the meantime.
      </p>

      <p className="mt-1 font-mono text-[11px] text-slate-400">
        Request {shortId(approval.approvalId)}
      </p>
    </div>
  );
}
