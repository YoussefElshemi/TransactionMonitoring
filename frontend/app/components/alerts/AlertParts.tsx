"use client";

import Link from "next/link";
import { ArrowRight, Layers, ShieldCheck } from "lucide-react";

import { Alert, Party, PartyRole, RuleSeverity } from "@/types";
import {
  SUPPRESSION_ASSURANCE,
  alertStatusText,
  openAlerts,
  suppressionReasonLabel,
  suppressionReasonText,
  worstAlertSeverity,
} from "@/lib/alerts";
import { SeverityBadge } from "@/components/SeverityBadge";
import { partyName } from "@/lib/parties";
import { shortId } from "@/lib/resources";
import { RoleChip } from "@/components/parties/PartyParts";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

import { useFormat } from "@/lib/useFormat";
import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const OPEN =
  "border-attention-200 bg-attention-50 text-attention-700 ring-1 ring-inset ring-attention-200";
const SUPPRESSED = "border-inflight-200 bg-inflight-50 text-inflight-700";
const CLOSED = "border-transparent bg-settled-50 text-settled-700";
const UNKNOWN = "border-slate-200 bg-slate-100 text-slate-500";

const alertStatusStyles: Record<string, string> = {
  Open: OPEN,
  Suppressed: SUPPRESSED,
  Closed: CLOSED,
};

export function AlertStatusBadge({
  status,
  className,
}: {
  status: string;
  className?: string;
}) {
  return (
    <Badge
      variant="outline"
      className={cn(
        "rounded-md px-2 py-0.5 font-mono text-[11px] font-medium",
        alertStatusStyles[status] ?? UNKNOWN,
        className
      )}
      title={alertStatusText(status) ?? undefined}
    >
      {status}
    </Badge>
  );
}

export function AlertSubject({
  partyId,
  partyRole,
  party,
  className,
}: {
  partyId: string | null;
  partyRole: PartyRole | string | null;
  party?: Party;
  className?: string;
}) {
  const t = useTranslation();
  if (!partyId) {
    return (
      <span
        className={cn("font-mono text-xs text-slate-400", className)}
        title={t("alerts.thisOrganisationResolvesNoParties")}
      >
        no subject
      </span>
    );
  }

  return (
    <span className={cn("flex min-w-0 flex-col gap-1", className)}>
      <Link
        href={`/parties/${partyId}`}
        className="min-w-0 truncate text-sm text-brand-800 hover:underline"
      >
        {party ? partyName(party) : shortId(partyId)}
      </Link>
      {partyRole && <RoleChip role={partyRole} muted />}
    </span>
  );
}

export function SuppressionNote({
  alert,
  className,
}: {
  alert: Alert;
  className?: string;
}) {
  const t = useTranslation();
  if (alert.status !== "Suppressed") return null;

  return (
    <div
      className={cn(
        "rounded-md border border-inflight-200 bg-inflight-50 px-3 py-2.5",
        className
      )}
    >
      <div className="flex items-start gap-2">
        <Layers className="mt-0.5 size-4 flex-shrink-0 text-inflight-700" />
        <div className="min-w-0 space-y-2">
          <div>
            <p className="text-sm font-medium text-inflight-700">
              {t("alerts.foldedIntoAnAlertThat")}
            </p>
            <p className="mt-0.5 text-xs text-inflight-700">
              {alert.suppressionReason
                ? suppressionReasonText(alert.suppressionReason)
                : "No reason was recorded against this suppression."}
            </p>
          </div>

          <p className="flex items-start gap-1.5 text-xs text-inflight-700">
            <ShieldCheck className="mt-0.5 size-3.5 flex-shrink-0" />
            <span>{SUPPRESSION_ASSURANCE}</span>
          </p>

          {alert.suppressedByAlertId ? (
            <Link
              href={`/alerts/${alert.suppressedByAlertId}`}
              className="inline-flex items-center gap-1.5 text-sm font-medium text-brand-800 hover:underline"
            >
              {t("alerts.openTheAlertThisFolded")}
              <ArrowRight className="size-3.5" />
            </Link>
          ) : (
            <p className="text-xs text-inflight-700">
              {t("alerts.noAlertWasRecordedAs")}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

export function SuppressionChip({ alert }: { alert: Alert }) {
  if (alert.status !== "Suppressed" || !alert.suppressionReason) return null;

  return (
    <span
      className="inline-flex items-center rounded px-1.5 py-0.5 font-mono text-[11px] text-inflight-700"
      title={suppressionReasonText(alert.suppressionReason)}
    >
      {suppressionReasonLabel(alert.suppressionReason)}
    </span>
  );
}

export function RaisedAt({
  alert,
  className,
}: {
  alert: Alert;
  className?: string;
}) {
  const format = useFormat();
  const raised = new Date(alert.raisedAt);
  const created = new Date(alert.createdAt);

  const backfilled = Math.abs(created.getTime() - raised.getTime()) > 1000;

  return (
    <span
      className={cn("text-sm text-slate-600", className)}
      title={
        backfilled
          ? `Raised ${format.instant(raised)}, recorded ${format.instant(created)} — this row was written later than the finding it describes.`
          : undefined
      }
    >
      <LocalTime value={alert.raisedAt} />
      {backfilled && (
        <span className="ml-1.5 font-mono text-[11px] text-slate-400">
          backfilled
        </span>
      )}
    </span>
  );
}

export function CaseAlertSummary({
  alerts,
  caseSeverity,
}: {
  alerts: Alert[];
  caseSeverity: RuleSeverity | string;
}) {
  const t = useTranslation();
  const format = useFormat();

  if (alerts.length === 0) {
    return <span className="font-mono text-xs text-slate-400">—</span>;
  }

  const open = openAlerts(alerts).length;
  const worst = worstAlertSeverity(alerts);

  return (
    <span className="flex flex-col gap-1">
      <span className="font-mono text-xs text-slate-600">
        {t(
          alerts.length === 1 ? "alerts.countOne" : "alerts.countMany",
          format.count(alerts.length)
        )}
        {open > 0 && (
          <span className="text-attention-700">
            {t("alerts.countOpen", format.count(open))}
          </span>
        )}
      </span>
      {worst && worst !== caseSeverity && (
        <span
          className="flex items-center gap-1"
          title={t("alerts.aMoreSevereFindingJoined")}
        >
          <span className="font-mono text-[10px] uppercase tracking-wide text-slate-400">
            holds
          </span>
          <SeverityBadge severity={worst} />
        </span>
      )}
    </span>
  );
}
