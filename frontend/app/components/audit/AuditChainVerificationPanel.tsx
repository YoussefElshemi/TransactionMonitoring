"use client";

import { ReactNode, useState } from "react";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";
import { ShieldAlert, ShieldCheck, TriangleAlert } from "lucide-react";

import { AuditChainVerification } from "@/types";
import { reportApiError, unwrap } from "@/lib/api";
import {
  AUDIT_BREAK_COPY,
  auditVerificationPath,
  auditBreakKind,
  auditIntactCaveat,
  auditRangeParams,
} from "@/lib/audit";
import { CopyableText } from "@/components/apiClients/CopyableText";
import { DateRangePicker } from "@/components/DateRangePicker";
import { LocalTime } from "@/components/LocalTime";
import { AuditExportButton } from "@/components/audit/AuditExportButton";
import { usePermissions } from "@/context/PermissionsContext";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function AuditChainVerificationPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId?: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const canVerify = holds("audit-logs:verify");
  const canExport = holds("audit-logs:export");

  const [range, setRange] = useState<DateRange | undefined>(undefined);
  const [verification, setVerification] =
    useState<AuditChainVerification | null>(null);
  const [verifying, setVerifying] = useState(false);
  const [exportRefusal, setExportRefusal] = useState<string | null>(null);

  const handleVerify = async () => {
    setVerifying(true);

    try {
      const params = auditRangeParams(range).toString();
      const response = await apiFetch(
        `${auditVerificationPath(organizationId)}${params ? `?${params}` : ""}`,
        { method: "GET", credentials: "include" }
      );

      if (!response.ok) {
        setVerification(null);
        await reportApiError(response, {
          fallback: t("audit.auditChainCouldNotVerified"),
          onUnauthorized,
          toastId: "audit-verification",
        });
        return;
      }

      setVerification(await unwrap<AuditChainVerification>(response));
    } catch {
      setVerification(null);
      toast.error(t("audit.auditChainCouldNotVerified"), {
        id: "audit-verification",
      });
    } finally {
      setVerifying(false);
    }
  };

  return (
    <div className="space-y-4">
      <Card>
        <CardContent className="space-y-4 p-5">
          <div>
            <h3 className="font-mono text-sm font-medium tracking-tight text-slate-900">
              {t("audit.chainVerification")}
            </h3>
            <p className="mt-1 max-w-3xl text-sm text-slate-500">
              Recomputes every audit entry in the range from its own contents,
              and checks that it still hashes to the digest stored beside it and
              still names the entry before it. The signed nightly anchors are
              checked against the chain in the same pass. Leave the dates clear
              to check the whole chain.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <DateRangePicker value={range} onChange={setRange} />
            <Button
              onClick={handleVerify}
              disabled={verifying || !canVerify}
              title={
                canVerify
                  ? undefined
                  : t("audit.runningCheckNeedsAuditLogs")
              }
            >
              <ShieldCheck />
              {verifying ? "Checking..." : "Verify chain"}
            </Button>
          </div>

          {verifying ? (
            <div className="space-y-2">
              <Skeleton className="h-20 w-full" />
              <Skeleton className="h-4 w-72" />
            </div>
          ) : verification ? (
            <Verdict verification={verification} />
          ) : (
            <p className="text-xs text-slate-400">
              Nothing has been checked yet in this session. A verification reads
              the chain and writes nothing, so it can be run as often as
              somebody wants it run.
            </p>
          )}
        </CardContent>
      </Card>

      {canExport && (
      <Card>
        <CardContent className="space-y-4 p-5">
          <div>
            <h3 className="font-mono text-sm font-medium tracking-tight text-slate-900">
              {t("audit.export")}
            </h3>
            <p className="mt-1 max-w-3xl text-sm text-slate-500">
              The whole organization&apos;s trail over the dates above, as a CSV
              carrying each entry&apos;s snapshot and both of its digests — so
              somebody holding the file can verify the chain without asking us
              for anything. One record&apos;s own trail is exported from the
              History panel on that record.
            </p>
          </div>

          <AuditExportButton
            scope={{ range }}
            organizationId={organizationId}
            label={t("audit.exportAuditLog")}
            size="default"
            onRefused={setExportRefusal}
            onUnauthorized={onUnauthorized}
          />

          {exportRefusal && (
            <div className="flex gap-2.5 rounded-md border border-attention-200 bg-attention-50 px-3 py-2.5 ring-1 ring-inset ring-attention-200">
              <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
              <div className="space-y-1 text-sm">
                <p className="font-medium text-attention-700">
                  {t("audit.tooManyEntriesToExport")}
                </p>
                <p className="text-slate-600">{exportRefusal}</p>
                <p className="text-xs text-slate-500">
                  Nothing was truncated. A file somebody believes is a whole
                  range and is not is worse than no file at all — they read an
                  absent entry as evidence the thing never happened.
                </p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
      )}
    </div>
  );
}

function Verdict({ verification }: { verification: AuditChainVerification }) {
  const t = useTranslation();
  const format = useFormat();
  const breakKind = auditBreakKind(verification);
  const caveat = auditIntactCaveat(verification);

  return (
    <div className="space-y-3">
      {breakKind ? (
        <div className="space-y-2 rounded-md bg-critical-solid px-4 py-3 text-critical-solid-foreground">
          <p className="flex items-center gap-2 font-mono text-sm font-medium">
            <ShieldAlert className="size-4 flex-shrink-0" />
            {AUDIT_BREAK_COPY[breakKind].title}
          </p>
          <p className="text-sm opacity-90">
            {AUDIT_BREAK_COPY[breakKind].body}
          </p>
        </div>
      ) : (
        <div className="space-y-2 rounded-md bg-settled-50 px-4 py-3">
          <p className="flex items-center gap-2 font-mono text-sm font-medium text-settled-700">
            <ShieldCheck className="size-4 flex-shrink-0" />
            {t("audit.chainIntact")}
          </p>
          <p className="text-sm text-slate-600">
            Every entry checked hashes to the digest recorded beside it and
            names the entry before it, and every anchor checked agrees with the
            position it witnessed.
          </p>
          {caveat && (
            <p className="text-xs text-slate-500">{caveat}</p>
          )}
        </div>
      )}

      <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
        <Stat label={t("audit.entriesChecked")}>
          {format.count(verification.entryCount)}
          {(breakKind === "entry" || breakKind === "both") && (
            <span className="ml-1.5 font-sans text-xs font-normal text-slate-500">
              before it stopped
            </span>
          )}
        </Stat>
        <Stat label={t("audit.anchorsChecked")}>
          {format.count(verification.anchorCount)}
        </Stat>
        <Stat label={t("audit.verifiedAt")}>
          <LocalTime value={verification.verifiedAt} />
        </Stat>
        <Stat label={t("common.organization")}>
          <span className="break-all text-xs text-slate-500">
            {verification.organizationId}
          </span>
        </Stat>

        {verification.firstBrokenAuditLogId && (
          <div className="sm:col-span-2">
            <dt className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("audit.firstBrokenEntry")}
            </dt>
            <dd className="mt-1 space-y-1">
              <CopyableText value={verification.firstBrokenAuditLogId} />
              {verification.firstBrokenSequence !== null && (
                <p className="text-xs text-slate-500">
                  Position #{format.count(verification.firstBrokenSequence)}{" "}
                  in the chain — the number shown under the entry on its
                  record&apos;s History panel.
                </p>
              )}
            </dd>
          </div>
        )}

        {verification.firstBrokenAuditAnchorId && (
          <div className="sm:col-span-2">
            <dt className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("audit.firstBrokenAnchor")}
            </dt>
            <dd className="mt-1">
              <CopyableText value={verification.firstBrokenAuditAnchorId} />
            </dd>
          </div>
        )}
      </dl>
    </div>
  );
}

function Stat({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <dt className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
      </dt>
      <dd className="mt-0.5 font-mono text-sm text-slate-900">{children}</dd>
    </div>
  );
}
