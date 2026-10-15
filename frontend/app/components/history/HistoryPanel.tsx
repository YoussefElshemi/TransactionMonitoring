"use client";

import { ReactNode } from "react";
import { Bot } from "lucide-react";

import { usePermissions } from "@/context/PermissionsContext";
import { AuditEntityType, AuditLog, AuditLogChange } from "@/types";
import { DateRangePicker } from "@/components/DateRangePicker";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { AuditExportButton } from "@/components/audit/AuditExportButton";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

import { AuditChainStamp } from "./AuditChainStamp";
import { AuditValue } from "./AuditValue";
import { TextDiff } from "./TextDiff";
import {
  fieldKind,
  fieldLabel,
  isDeletedField,
  Lifecycle,
  lifecycleOf,
} from "./auditFields";
import { ENTITY_NOUNS } from "./paths";
import type { ActorLookup } from "./useAuditHistory";
import { useAuditHistory } from "./useAuditHistory";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export interface HistoryChangeContext {
  log: AuditLog;
  isInitialSnapshot: boolean;
}

export interface HistoryPanelProps {
  entityType: AuditEntityType;
  path: string;
  entityId?: string;
  organizationId?: string;
  onUnauthorized: () => void;
  enabled?: boolean;
  renderChange?: (
    change: AuditLogChange,
    context: HistoryChangeContext
  ) => ReactNode | null;
  scopeNote?: ReactNode;
  lifecycleNotes?: Partial<Record<NonNullable<Lifecycle>, string>>;
  className?: string;
}

export function HistoryPanel({
  entityType,
  path,
  entityId,
  organizationId,
  onUnauthorized,
  enabled = true,
  renderChange,
  scopeNote,
  lifecycleNotes,
  className,
}: HistoryPanelProps) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const noun = ENTITY_NOUNS[entityType];

  const {
    logs,
    loading,
    forbidden,
    failed,
    pageNumber,
    pageCount,
    totalCount,
    range,
    setRange,
    setPageNumber,
    reload,
    actors,
  } = useAuditHistory({ path, enabled, onUnauthorized });

  if (forbidden) {
    return (
      <p className={cn("py-12 text-center text-sm text-slate-500", className)}>
        You do not have permission to view this {noun}&apos;s history.
      </p>
    );
  }

  const filtered = Boolean(range?.from);

  return (
    <div className={cn("space-y-4", className)}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">
          {loading && logs.length === 0
            ? "Loading history…"
            : `${totalCount} ${totalCount === 1 ? "entry" : "entries"}${
                filtered ? " in range" : ""
              }`}
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <DateRangePicker value={range} onChange={setRange} />
          {entityId && holds("audit-logs:export") && (
            <AuditExportButton
              scope={{ range, entityType, entityId }}
              organizationId={organizationId}
              onUnauthorized={onUnauthorized}
            />
          )}
        </div>
      </div>

      {loading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, index) => (
            <div key={index} className="space-y-2">
              <Skeleton className="h-4 w-64" />
              <Skeleton className="h-16 w-full" />
            </div>
          ))}
        </div>
      ) : failed ? (
        <div className="rounded-md border border-dashed border-slate-200 bg-card py-12 text-center">
          <p className="text-sm text-slate-500">
            {t("history.historyCouldNotBeLoaded")}
          </p>
          <Button variant="outline" size="sm" className="mt-3" onClick={reload}>
            {t("common.retry")}
          </Button>
        </div>
      ) : logs.length === 0 ? (
        <div className="rounded-md border border-dashed border-slate-200 bg-card px-6 py-12 text-center">
          <p className="text-sm text-slate-500">
            {filtered
              ? `No changes to this ${noun} in the selected dates.`
              : `No changes recorded for this ${noun}.`}
          </p>
          {scopeNote && (
            <p className="mx-auto mt-2 max-w-lg text-xs text-slate-400">
              {scopeNote}
            </p>
          )}
        </div>
      ) : (
        <>
          <ol className="relative space-y-6 border-l border-slate-200 pl-6">
            {logs.map(({ auditLog: log }) => (
              <HistoryEntry
                key={log.auditLogId}
                log={log}
                actors={actors}
                canReachAdminPages={holds("platform:administer")}
                renderChange={renderChange}
                lifecycleNotes={lifecycleNotes}
              />
            ))}
          </ol>
          {scopeNote && (
            <p className="text-xs text-slate-400">{scopeNote}</p>
          )}
        </>
      )}

      {logs.length > 0 && pageCount > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber(Math.max(1, pageNumber - 1))}
          pageNumber={pageNumber}
          totalPages={pageCount}
          handleNextPage={() =>
            setPageNumber(Math.min(pageCount, pageNumber + 1))
          }
        />
      )}
    </div>
  );
}

const DOT_STYLES: Record<AuditLog["operation"], string> = {
  Created: "bg-settled-500",
  Updated: "bg-attention-500",
  Deleted: "bg-critical-500",
  Revealed: "bg-inflight-500",
};

const LIFECYCLE_NOTES: Record<NonNullable<Lifecycle>, string> = {
  softDeleted:
    "Soft delete — the record is hidden from lists but kept, and can be recovered.",
  recovered: "Restored after an earlier soft delete.",
};

const revealReason = (changes: AuditLogChange[]): string | null => {
  const reason = changes.find((change) => change.field === "Reason");
  return typeof reason?.newValue === "string" && reason.newValue.length > 0
    ? reason.newValue
    : null;
};

function HistoryEntry({
  log,
  actors,
  canReachAdminPages,
  renderChange,
  lifecycleNotes,
}: {
  log: AuditLog;
  actors: ActorLookup;
  canReachAdminPages: boolean;
  renderChange?: HistoryPanelProps["renderChange"];
  lifecycleNotes?: HistoryPanelProps["lifecycleNotes"];
}) {
  const t = useTranslation();
  const isInitialSnapshot = log.operation === "Created";
  const isReveal = log.operation === "Revealed";
  const lifecycle =
    isInitialSnapshot || isReveal ? null : lifecycleOf(log.changes);

  const changes = lifecycle
    ? log.changes.filter((change) => !isDeletedField(change.field))
    : log.changes;

  const operationLabel =
    lifecycle === "recovered" ? "Recovered" : log.operation;

  const timestamp = new Date(log.createdAt);
  const format = useFormat();

  return (
    <li className="relative">
      <span
        className={cn(
          "absolute -left-[1.9375rem] top-1.5 size-3 rounded-full ring-4 ring-white",
          DOT_STYLES[log.operation] ?? "bg-slate-400"
        )}
      />

      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <StatusBadge status={log.operation} label={operationLabel} />
        <Actor log={log} actors={actors} />
        <span className="text-slate-300">·</span>
        <time
          dateTime={log.createdAt}
          title={format.instant(timestamp)}
          className="text-xs text-slate-500"
        >
          {format.relative(timestamp)}
        </time>
      </div>

      {lifecycle && (
        <p className="mt-1.5 text-xs text-slate-500">
          {lifecycleNotes?.[lifecycle] ?? LIFECYCLE_NOTES[lifecycle]}
        </p>
      )}

      {isReveal ? (
        <RevealedEntry reason={revealReason(log.changes)} />
      ) : changes.length === 0 ? (
        lifecycle ? null : (
          <p className="mt-2 text-sm text-slate-400">
            No field changes — the record was saved without altering an audited
            field.
          </p>
        )
      ) : (
        <div className="mt-2 overflow-hidden rounded-md border border-slate-200 bg-card">
          {isInitialSnapshot && (
            <p className="border-b border-slate-100 bg-slate-50 px-3 py-1.5 font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("history.initialValues")}
            </p>
          )}
          <dl className="divide-y divide-slate-100">
            {changes.map((change) => (
              <ChangeRow
                key={change.field}
                change={change}
                log={log}
                isInitialSnapshot={isInitialSnapshot}
                actors={actors}
                canReachAdminPages={canReachAdminPages}
                renderChange={renderChange}
              />
            ))}
          </dl>
        </div>
      )}

      <AuditChainStamp
        sequence={log.sequence}
        hash={log.hash}
        previousHash={log.previousHash}
      />
    </li>
  );
}

function RevealedEntry({ reason }: { reason: string | null }) {
  return (
    <p className="mt-1.5 text-sm text-slate-600">
      Payload revealed
      {reason ? (
        <>
          {" — "}
          <span className="text-slate-900">{reason}</span>
        </>
      ) : (
        <span className="text-slate-400"> — no reason recorded</span>
      )}
    </p>
  );
}

function Actor({ log, actors }: { log: AuditLog; actors: ActorLookup }) {
  const t = useTranslation();
  if (log.changedByApiClientId !== null) {
    const apiClientId = log.changedByApiClientId;
    const name = actors.nameOfApiClient(apiClientId);
    return (
      <span
        className="inline-flex items-center gap-1.5 text-sm text-slate-700"
        title={`API client ${apiClientId}`}
      >
        <Bot className="size-3.5 flex-shrink-0 text-slate-400" />
        {name ?? `API client ${apiClientId.slice(0, 8)}…`}
      </span>
    );
  }

  if (log.changedByUserId === null) {
    return (
      <span
        className="text-sm text-slate-500"
        title={t("history.changedByABackgroundJob")}
      >
        {t("history.system")}
      </span>
    );
  }

  const userId = log.changedByUserId;
  const name = actors.nameOf(userId);
  return (
    <span className="text-sm text-slate-700" title={userId}>
      {name ?? `User ${userId.slice(0, 8)}…`}
    </span>
  );
}

const LONG_VALUE_CHARS = 80;

const isLongValue = (value: unknown): boolean =>
  typeof value === "string" && value.length > LONG_VALUE_CHARS;

const asText = (value: unknown): string | null => {
  if (value === null || value === undefined) return null;
  if (typeof value === "string") {
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }
  return JSON.stringify(value, null, 2);
};

function ChangeRow({
  change,
  log,
  isInitialSnapshot,
  actors,
  canReachAdminPages,
  renderChange,
}: {
  change: AuditLogChange;
  log: AuditLog;
  isInitialSnapshot: boolean;
  actors: ActorLookup;
  canReachAdminPages: boolean;
  renderChange?: HistoryPanelProps["renderChange"];
}) {
  const custom = renderChange?.(change, { log, isInitialSnapshot });

  const long =
    fieldKind(change.field) === "longText" ||
    isLongValue(change.oldValue) ||
    isLongValue(change.newValue);

  return (
    <div
      className={cn(
        "gap-2 px-3 py-2",
        custom || long ? "space-y-2" : "grid sm:grid-cols-[11rem_1fr]"
      )}
    >
      <dt className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {fieldLabel(change.field)}
      </dt>
      <dd className="min-w-0">
        {custom ? (
          custom
        ) : long ? (
          <TextDiff
            before={isInitialSnapshot ? null : asText(change.oldValue)}
            after={asText(change.newValue)}
          />
        ) : (
          <div className="flex flex-wrap items-center gap-2">
            {!isInitialSnapshot && (
              <>
                <AuditValue
                  field={change.field}
                  value={change.oldValue}
                  actors={actors}
                  canReachAdminPages={canReachAdminPages}
                  muted
                />
                <span aria-hidden className="text-slate-300">
                  →
                </span>
              </>
            )}
            <AuditValue
              field={change.field}
              value={change.newValue}
              actors={actors}
              canReachAdminPages={canReachAdminPages}
            />
          </div>
        )}
      </dd>
    </div>
  );
}
