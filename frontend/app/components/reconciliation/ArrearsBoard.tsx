"use client";

import { Fragment, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Gavel, TriangleAlert } from "lucide-react";

import type { DunningStage, DunningState } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  DUNNING_STAGES,
  LADDER_IS_NOTICES_ONLY,
  NEVER_ATTEMPTED,
  NOTHING_HAS_BEEN_RESTRICTED,
  RESOLVED_ROWS_ARE_THE_RECOVERY_FIGURE,
  arrearsInWorkingOrder,
  daysLate,
  decisionsWaiting,
  dunningStageHintKey,
  dunningStageLabelKey,
  dunningStageTone,
  formatOutstanding,
  hasBeenAttempted,
  hasScheduledEvent,
  isResolved,
  needsDecision,
  nextEventDescriptionKey,
  stageIndex,
  CURRENCY_DID_NOT_TRAVEL,
} from "@/lib/dunning";
import { usePermissions } from "@/context/PermissionsContext";
import { useOrganizationNames } from "@/lib/useOrganizationNames";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useTranslation } from "@/context/LocalizationContext";

export function ArrearsBoard({
  filterOrganizationId,
  onUnauthorized,
}: {
  filterOrganizationId?: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();

  const mayRead = holds("dunning:read") && holds("platform:administer");

  const [states, setStates] = useState<DunningState[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [includeResolved, setIncludeResolved] = useState(false);
  const [opened, setOpened] = useState<string | null>(null);

  const now = new Date();

  const organizationName = useOrganizationNames(
    filterOrganizationId === undefined
      ? [...new Set(states.map((state) => state.organizationId))]
      : []
  );

  const fetchArrears = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: "25",
        IncludeResolved: String(includeResolved),
      });
      if (filterOrganizationId !== undefined) {
        query.set("FilterOrganizationId", filterOrganizationId);
      }

      const response = await apiFetch(
        `${API_BASE}/reconciliation/arrears?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<DunningState>(response);
        setStates(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("reconciliation.arrearsCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-arrears",
        });
      }
    } catch {
      toast.error(t("reconciliation.arrearsCouldNotBeRead"), {
        id: "error-fetching-arrears",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, includeResolved, filterOrganizationId, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchArrears();
    else setLoading(false);
  }, [mayRead, fetchArrears]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")}{" "}
          <span className="font-mono text-xs">dunning:read</span>{" "}
          {t("reconciliation.and")}{" "}
          <span className="font-mono text-xs">platform:administer</span>.
        </p>
      </div>
    );
  }

  const ordered = arrearsInWorkingOrder(states);
  const waiting = decisionsWaiting(states);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Gavel className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("reconciliation.arrears")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("reconciliation.everyLateInvoiceAndHowFar")}
            </p>
            {waiting > 0 && (
              <p className="mt-1 text-xs text-critical-700">
                {t("reconciliation.decisionsWaiting", waiting)}
              </p>
            )}
          </div>
        </div>

        <Button
          variant="ghost"
          size="sm"
          className={cn(
            "h-7 rounded-lg border border-slate-200 px-2.5 text-xs",
            includeResolved && "bg-slate-100 text-slate-900"
          )}
          onClick={() => {
            setIncludeResolved((shown) => !shown);
            setPageNumber(1);
          }}
        >
          {t("reconciliation.showResolved")}
        </Button>
      </div>

      <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
        {t(LADDER_IS_NOTICES_ONLY)}
      </p>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              {filterOrganizationId === undefined && (
                <TableHead className="w-48">
                  {t("reconciliation.customer")}
                </TableHead>
              )}
              <TableHead className="w-36">{t("billing.invoice")}</TableHead>
              <TableHead className="w-44">{t("reconciliation.stage")}</TableHead>
              <TableHead className="w-32 text-right">
                {t("billing.outstanding")}
              </TableHead>
              <TableHead className="w-32">{t("reconciliation.late")}</TableHead>
              <TableHead className="w-44">
                {t("reconciliation.whatHappensNext")}
              </TableHead>
              <TableHead className="w-24">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({
                    length: filterOrganizationId === undefined ? 7 : 6,
                  }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : ordered.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={filterOrganizationId === undefined ? 7 : 6}
                  className="py-10 text-center"
                >
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("reconciliation.nobodyIsBehind")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {includeResolved
                      ? t("reconciliation.nothingHasEverBeenChased")
                      : t("reconciliation.noOpenArrears")}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              ordered.map((state) => {
                const decision = needsDecision(state) && !isResolved(state);
                const late = daysLate(state, now);
                const isOpen = opened === state.dunningStateId;

                return (
                  <Fragment key={state.dunningStateId}>
                    <TableRow
                      className={cn(
                        "hover:bg-slate-50/50",
                        decision && "bg-critical-50/40",
                        isResolved(state) && "opacity-60"
                      )}
                    >
                      {filterOrganizationId === undefined && (
                        <TableCell className="text-sm">
                          <Link
                            href={`/admin/organizations/${state.organizationId}`}
                            className="text-slate-900 underline-offset-2 hover:underline"
                          >
                            {organizationName(state.organizationId) ??
                              state.organizationId.slice(0, 8)}
                          </Link>
                        </TableCell>
                      )}

                      <TableCell className="font-mono text-xs text-slate-700">
                        {state.invoiceNumber ?? state.invoiceId.slice(0, 8)}
                      </TableCell>

                      <TableCell>
                        <div className="flex flex-col items-start gap-1">
                          <ToneBadge
                            tone={dunningStageTone(state.stage)}
                            title={
                              dunningStageHintKey(state.stage)
                                ? t(dunningStageHintKey(state.stage))
                                : undefined
                            }
                          >
                            {t(dunningStageLabelKey(state.stage))}
                          </ToneBadge>
                          {isResolved(state) && (
                            <span className="text-[11px] text-slate-500">
                              {t("reconciliation.resolved")}{" "}
                              <LocalTime value={state.resolvedAt!} dateOnly />
                            </span>
                          )}
                        </div>
                      </TableCell>

                      <TableCell className="text-right font-mono text-sm text-slate-900">
                        {formatOutstanding(state)}
                        {state.currency === null && (
                          <span
                            className="block font-sans text-[11px] font-normal text-slate-400"
                            title={t(CURRENCY_DID_NOT_TRAVEL)}
                          >
                            {t("reconciliation.currencyUnknown")}
                          </span>
                        )}
                      </TableCell>

                      <TableCell className="text-xs">
                        {late === null ? (
                          <span className="text-slate-400">
                            {t("reconciliation.dueDateUnknown")}
                          </span>
                        ) : (
                          <span
                            className={
                              decision ? "text-critical-700" : "text-slate-600"
                            }
                          >
                            {t("reconciliation.daysLate", late)}
                          </span>
                        )}
                      </TableCell>

                      <TableCell className="text-xs text-slate-600">
                        {t(nextEventDescriptionKey(state))}
                        {hasScheduledEvent(state) && (
                          <span className="block text-[11px] text-slate-400">
                            <LocalTime value={state.nextAttemptAt} dateOnly />
                          </span>
                        )}
                      </TableCell>

                      <TableCell>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-8 text-xs"
                          onClick={() =>
                            setOpened(isOpen ? null : state.dunningStateId)
                          }
                        >
                          {isOpen
                            ? t("reconciliation.hide")
                            : t("reconciliation.history")}
                        </Button>
                      </TableCell>
                    </TableRow>

                    {isOpen && (
                      <TableRow className="hover:bg-transparent">
                        <TableCell
                          colSpan={filterOrganizationId === undefined ? 7 : 6}
                          className="bg-slate-50/60 p-4"
                        >
                          <NoticeHistory state={state} />
                        </TableCell>
                      </TableRow>
                    )}
                  </Fragment>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      <p className="text-xs text-slate-400">
        {t(RESOLVED_ROWS_ARE_THE_RECOVERY_FIGURE)}
      </p>

      {ordered.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}
    </div>
  );
}

function NoticeHistory({ state }: { state: DunningState }) {
  const t = useTranslation();
  const position = stageIndex(state.stage);

  return (
    <div className="space-y-4">
      <div>
        <p className="font-mono text-[11px] uppercase tracking-widest text-slate-500">
          {t("reconciliation.theLadder")}
        </p>
        <ol className="mt-2 flex flex-wrap gap-1">
          {DUNNING_STAGES.map((stage: DunningStage, index) => {
            const reached = index <= position;
            const here = index === position;

            return (
              <li
                key={stage}
                className={cn(
                  "rounded-md border px-2 py-1 text-[11px]",
                  here
                    ? "border-slate-900 bg-slate-900 text-slate-50"
                    : reached
                      ? "border-slate-300 bg-slate-100 text-slate-600"
                      : "border-dashed border-slate-200 text-slate-400"
                )}
                aria-current={here ? "step" : undefined}
              >
                {t(dunningStageLabelKey(stage))}
                {here && (
                  <span className="ml-1 font-medium">
                    {t("reconciliation.here")}
                  </span>
                )}
              </li>
            );
          })}
        </ol>
        <p className="mt-2 max-w-3xl text-xs text-slate-500">
          {t(dunningStageHintKey(state.stage))}
        </p>
      </div>

      {needsDecision(state) && (
        <p className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700">
          <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" aria-hidden />
          <span>{t(NOTHING_HAS_BEEN_RESTRICTED)}</span>
        </p>
      )}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <HistoryFact label={t("reconciliation.due")}>
          {state.dueAt === null ? (
            <span className="text-slate-400">
              {t("reconciliation.notRecorded")}
            </span>
          ) : (
            <LocalTime value={state.dueAt} dateOnly />
          )}
        </HistoryFact>

        <HistoryFact label={t("reconciliation.lastNotice")}>
          {state.lastNoticedAt === null ? (
            <span className="text-slate-400">
              {t("reconciliation.nothingSentYet")}
            </span>
          ) : (
            <LocalTime value={state.lastNoticedAt} dateOnly />
          )}
        </HistoryFact>

        <HistoryFact label={t("reconciliation.collectionAttempts")}>
          <span className="font-mono">{state.collectionAttempts}</span>
          {state.lastAttemptedAt !== null && (
            <span className="block text-[11px] text-slate-400">
              {t("reconciliation.lastTried")}{" "}
              <LocalTime value={state.lastAttemptedAt} dateOnly />
            </span>
          )}
        </HistoryFact>

        <HistoryFact label={t("reconciliation.decisionRaised")}>
          {state.restrictionProposedAt === null ? (
            <span className="text-slate-400">
              {t("reconciliation.notRaised")}
            </span>
          ) : (
            <>
              <LocalTime value={state.restrictionProposedAt} dateOnly />
              <span className="block text-[11px] text-slate-500">
                {t("reconciliation.andTheDateTheyWereWarnedOf")}
              </span>
            </>
          )}
        </HistoryFact>
      </div>

      {!hasBeenAttempted(state) && (
        <p className="text-xs text-slate-500">{t(NEVER_ATTEMPTED)}</p>
      )}
    </div>
  );
}

function HistoryFact({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-card p-3">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-500">
        {label}
      </p>
      <p className="mt-1 text-sm text-slate-900">{children}</p>
    </div>
  );
}
