"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Plus } from "lucide-react";

import type { ServiceCredit } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { canOperateStatus } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { formatUptime } from "@/lib/availability";
import {
  ISSUING_IS_THE_EXCEPTION,
  ONLY_MISSED_LABEL,
  WHY_MET_MONTHS_ARE_LISTED,
  creditTone,
  formatCreditAmount,
  issueRefusal,
  metTheTarget,
  monthLabel,
  newestMonthFirst,
  paperworkSentence,
} from "@/lib/serviceCredits";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import Pagination from "@/components/Pagination";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function ServiceCreditsPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants, holds } = usePermissions();

  const canRead = holds("service-credits:read");
  const canIssue = canOperateStatus(grants, "service-credits:issue");
  const canOpenCreditNotes =
    holds("credit-notes:read") && holds("platform:administer");

  const [credits, setCredits] = useState<ServiceCredit[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [hideMet, setHideMet] = useState(false);

  const [issueOpen, setIssueOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [month, setMonth] = useState("");
  const [year, setYear] = useState("");

  const submission = useIdempotencyKey();

  const fetchCredits = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/service-credits?PageNumber=${pageNumber}&PageSize=24&IncludeMet=${!hideMet}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<ServiceCredit>(response);
        setCredits(page.data);
        setTotalPages(page.metadata.pageCount);
      } else if (response.status !== 403) {
        await reportApiError(response, {
          fallback: t("availability.errorOccurredFetchingServiceCredits"),
          onUnauthorized,
          toastId: "error-fetching-service-credits",
        });
      }
    } catch {
      toast.error(t("availability.errorOccurredFetchingServiceCredits"), {
        id: "error-fetching-service-credits",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, hideMet, onUnauthorized]);

  useEffect(() => {
    if (canRead) fetchCredits();
  }, [canRead, fetchCredits]);

  const refusal = issueRefusal({ month, year });

  const issue = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/service-credits`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            month: Number(month.trim()),
            year: Number(year.trim()),
          }),
        },
        { idempotencyKey: submission.current("issue") }
      );

      if (response.ok) {
        const issued = await unwrap<ServiceCredit>(response);
        submission.completed("issue");
        toast.success(t("availability.monthMeasured", monthLabel(issued)), {
          description: metTheTarget(issued)
            ? t("availability.monthMetTargetNothingOwed")
            : t(
            "availability.creditedComingTo",
            formatUptime(issued.creditPercentage),
            formatCreditAmount(issued)
          ),
        });
        setIssueOpen(false);
        setMonth("");
        setYear("");
        fetchCredits();
      } else {
        await reportApiError(response, {
          fallback: t("availability.errorOccurredIssuingCredit"),
          onUnauthorized,
          toastId: "error-issuing-credit",
        });
      }
    } catch {
      toast.error(t("availability.errorOccurredIssuingCredit"), {
        id: "error-issuing-credit",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!canRead) {
    return (
      <Card>
        <CardContent className="p-6">
          <p className="text-sm leading-relaxed text-slate-500">
            {t("availability.readingServiceCreditsNeeds")} <code>service-credits:read</code>.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <p className="max-w-2xl text-xs leading-relaxed text-slate-500">
        {WHY_MET_MONTHS_ARE_LISTED}
      </p>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input
            type="checkbox"
            checked={hideMet}
            onChange={(event) => {
              setHideMet(event.target.checked);
              setPageNumber(1);
            }}
            className="size-4 rounded border-slate-300 accent-brand-800"
          />
          {ONLY_MISSED_LABEL}
        </label>
        {canIssue && (
          <Button
            variant="outline"
            onClick={() => setIssueOpen(true)}
            className="gap-1.5"
          >
            <Plus className="size-4" />
            {t("availability.measureAMonth")}
          </Button>
        )}
      </div>

      {loading ? (
        <Skeleton className="h-64 w-full rounded-lg" />
      ) : credits.length === 0 ? (
        <Card>
          <CardContent className="p-6">
            <p className="text-sm leading-relaxed text-slate-500">
              {hideMet
                ? "No month has missed its target. That is the outcome this list exists to be able to prove — turn the filter off to see the record."
                : "No month has been measured for this organisation yet. Months are measured by the monthly run once a target has been agreed."}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("availability.month")}</TableHead>
                <TableHead>{t("availability.measured")}</TableHead>
                <TableHead>{t("availability.target")}</TableHead>
                <TableHead>{t("availability.credit2")}</TableHead>
                <TableHead>{t("common.amount")}</TableHead>
                <TableHead>{t("availability.paperwork")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {newestMonthFirst(credits).map((credit) => (
                <TableRow key={credit.serviceCreditId}>
                  <TableCell className="font-mono text-xs text-slate-700">
                    {monthLabel(credit)}
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-xs text-slate-700">
                        {formatUptime(credit.measuredUptimePercentage)}
                      </span>
                      <ToneBadge tone={creditTone(credit)}>
                        {metTheTarget(credit) ? "Met" : "Missed"}
                      </ToneBadge>
                    </div>
                  </TableCell>
                  <TableCell className="font-mono text-xs text-slate-500">
                    {formatUptime(credit.availabilityTarget)}
                  </TableCell>
                  <TableCell className="font-mono text-xs text-slate-700">
                    {formatUptime(credit.creditPercentage)}
                  </TableCell>
                  <TableCell className="font-mono text-xs text-slate-700">
                    {formatCreditAmount(credit)}
                  </TableCell>
                  <TableCell className="max-w-xs">
                    {credit.creditNoteId !== null && canOpenCreditNotes ? (
                      <Link
                        href={`/admin/organizations/${credit.organizationId}#Invoices`}
                        className="font-mono text-xs text-brand-800 underline-offset-4 hover:underline"
                        title={paperworkSentence(credit)}
                      >
                        {t("availability.creditNoteRaised")}
                      </Link>
                    ) : (
                      <span
                        className="text-xs leading-relaxed text-slate-500"
                        title={paperworkSentence(credit)}
                      >
                        {paperworkSentence(credit)}
                      </span>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={issueOpen} onOpenChange={setIssueOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("availability.measureAMonthByHand")}</DialogTitle>
            <DialogDescription>{ISSUING_IS_THE_EXCEPTION}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="flex flex-wrap gap-4">
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.month")}
                </label>
                <Input
                  value={month}
                  onChange={(event) => setMonth(event.target.value)}
                  inputMode="numeric"
                  placeholder="3"
                  className="mt-1.5 w-24"
                />
              </div>
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("availability.year")}
                </label>
                <Input
                  value={year}
                  onChange={(event) => setYear(event.target.value)}
                  inputMode="numeric"
                  placeholder="2026"
                  className="mt-1.5 w-32"
                />
              </div>
            </div>

            {refusal !== null && month.length + year.length > 0 && (
              <p className="text-sm leading-relaxed text-critical-700">
                {refusal}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIssueOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={issue} disabled={refusal !== null || saving}>
              {saving ? "Measuring…" : "Measure"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
