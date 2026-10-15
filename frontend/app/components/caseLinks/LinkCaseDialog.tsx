"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import { CaseResource, Paged } from "@/types";
import { API_BASE, reportApiError } from "@/lib/api";
import { DispositionBadge } from "@/components/DispositionBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { StatusBadge } from "@/components/StatusBadge";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const MAX_REASON = 2048;

const PAGE_SIZE = 8;

export function LinkCaseDialog({
  open,
  onOpenChange,
  caseId,
  excludedCaseIds,
  submitting,
  onSubmit,
  onUnauthorized,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  caseId: string;
  excludedCaseIds: ReadonlySet<string>;
  submitting: boolean;
  onSubmit: (linkedCaseId: string, reason: string | null) => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [term, setTerm] = useState("");
  const [candidates, setCandidates] = useState<CaseResource[]>([]);
  const [loading, setLoading] = useState(false);
  const [picked, setPicked] = useState<CaseResource | null>(null);
  const [reason, setReason] = useState("");

  const search = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PageNumber: "1",
      PageSize: String(PAGE_SIZE * 3),
    });
    if (term.trim()) params.set("FriendlyName", term.trim());

    try {
      const response = await apiFetch(`${API_BASE}/cases?${params}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = (await response.json()) as Paged<CaseResource>;
        setCandidates(page.data);
      } else {
        setCandidates([]);
        await reportApiError(response, {
          fallback: t("caseLinks.casesCouldNotSearched"),
          onUnauthorized,
          toastId: "error-case-picker",
        });
      }
    } catch {
      setCandidates([]);
      toast.error(t("caseLinks.casesCouldNotSearched"), { id: "error-case-picker" });
    } finally {
      setLoading(false);
    }
  }, [term, onUnauthorized]);

  useEffect(() => {
    if (!open) return;
    const timer = setTimeout(search, term ? 300 : 0);
    return () => clearTimeout(timer);
  }, [open, search, term]);

  useEffect(() => {
    if (!open) {
      setTerm("");
      setPicked(null);
      setReason("");
      setCandidates([]);
    }
  }, [open]);

  const eligible = candidates
    .filter(
      (resource) =>
        resource.case.caseId !== caseId &&
        !excludedCaseIds.has(resource.case.caseId)
    )
    .slice(0, PAGE_SIZE);

  const allExcluded = !loading && candidates.length > 0 && eligible.length === 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t("caseLinks.linkACase")}</DialogTitle>
          <DialogDescription>
            One connection, readable from both ends — the case you pick will
            show this one in its own list too.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <Input
            autoFocus
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            placeholder={t("caseLinks.searchCasesByName")}
            aria-label={t("caseLinks.searchCasesByName")}
          />

          <div
            role="listbox"
            aria-label={t("caseLinks.cases")}
            className="max-h-72 space-y-1.5 overflow-y-auto"
          >
            {loading ? (
              Array.from({ length: 3 }).map((_, index) => (
                <Skeleton key={index} className="h-14 w-full" />
              ))
            ) : eligible.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-500">
                {allExcluded
                  ? "Every case matching that is already linked to this one"
                  : term
                  ? "No case matches that"
                  : "No other case to link to"}
              </p>
            ) : (
              eligible.map((resource) => {
                const candidate = resource.case;
                const chosen = picked?.case.caseId === candidate.caseId;

                return (
                  <button
                    key={candidate.caseId}
                    type="button"
                    role="option"
                    aria-selected={chosen}
                    onClick={() => setPicked(chosen ? null : resource)}
                    className={cn(
                      "flex w-full flex-wrap items-center justify-between gap-x-3 gap-y-1 rounded-md border p-3 text-left transition-colors",
                      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800",
                      chosen
                        ? "border-brand-600 bg-brand-50"
                        : "border-slate-200 bg-card hover:border-slate-300 hover:bg-slate-50/50"
                    )}
                  >
                    <div className="min-w-0">
                      <span className="text-sm font-medium text-slate-900">
                        {candidate.friendlyName}
                      </span>
                      <p className="mt-0.5 text-xs text-slate-400">
                        opened <LocalTime value={candidate.createdAt} />
                      </p>
                    </div>
                    <div className="flex flex-shrink-0 flex-wrap items-center gap-2">
                      <SeverityBadge severity={candidate.severity} />
                      <StatusBadge status={candidate.status} />
                      {candidate.disposition && (
                        <DispositionBadge disposition={candidate.disposition} />
                      )}
                    </div>
                  </button>
                );
              })
            )}
          </div>

          <div>
            <label
              htmlFor="link-reason"
              className="font-mono text-xs uppercase tracking-widest text-slate-400"
            >
              {t("caseLinks.reasonOptional")}
            </label>
            <Textarea
              id="link-reason"
              value={reason}
              maxLength={MAX_REASON}
              onChange={(event) => setReason(event.target.value)}
              placeholder={t("caseLinks.sameRemitterSameWeek")}
              rows={2}
              className="mt-1"
            />
            <p className="mt-1 text-xs text-slate-500">
              {t("caseLinks.leaveItBlankWhereThe")}
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            disabled={!picked || submitting}
            onClick={() =>
              picked &&
              onSubmit(picked.case.caseId, reason.trim() ? reason.trim() : null)
            }
          >
            {submitting ? "Linking…" : "Link case"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
