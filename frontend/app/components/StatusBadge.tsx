"use client";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

import { useTranslation } from "@/context/LocalizationContext";

const SETTLED = "border-transparent bg-settled-50 text-settled-700";
const INFLIGHT = "border-inflight-200 bg-inflight-50 text-inflight-700";
const ATTENTION =
  "border-attention-200 bg-attention-50 text-attention-700 ring-1 ring-inset ring-attention-200";
const ELEVATED =
  "border-elevated-200 bg-elevated-100 text-elevated-700 ring-1 ring-inset ring-elevated-200";
const CRITICAL = "border-critical-solid bg-critical-solid text-critical-solid-foreground";
const INERT = "border-slate-200 bg-slate-100 text-slate-500";

const statusStyles: Record<string, string> = {
  Pending: ATTENTION,
  Assigned: INFLIGHT,
  Approved: SETTLED,
  Rejected: CRITICAL,
  Expired: INERT,
  InReview: INFLIGHT,
  Escalated: ELEVATED,
  PendingInformation: ATTENTION,
  OnHold: ATTENTION,
  PendingApproval: ATTENTION,
  Withdrawn: INERT,
  Draft: INFLIGHT,
  Filed: INFLIGHT,
  Acknowledged: SETTLED,
  NotFiled: INERT,
  Issued: SETTLED,
  Accepted: SETTLED,
  Revoked: INERT,
  Enabled: SETTLED,
  Active: SETTLED,
  Disabled: INERT,
  Running: INFLIGHT,
  InProgress: INFLIGHT,
  Completed: SETTLED,
  Cancelled: INERT,
  Failed: CRITICAL,
  Live: SETTLED,
  Deploying: INFLIGHT,
  Undeployed: ATTENTION,
  Created: SETTLED,
  Updated: ATTENTION,
  Deleted: CRITICAL,
  Revealed: INFLIGHT,
  OverdueInvoice: ATTENTION,
  Allow: SETTLED,
  Flag: ATTENTION,
  Review: INFLIGHT,
  Block: CRITICAL,
};

const statusKey = (status: string) => `status.${status.toLowerCase()}`;

export function StatusBadge({
  status,
  label,
}: {
  status: string;
  label?: string;
}) {
  const t = useTranslation();
  const classes = statusStyles[status] ?? INERT;

  const key = statusKey(String(status));
  const translated = t(key);
  const text = translated === key ? status : translated;

  return (
    <Badge
      variant="outline"
      className={cn(
        "rounded-md px-2 py-0.5 font-mono text-[11px] font-medium",
        classes
      )}
    >
      {label ?? text}
    </Badge>
  );
}
