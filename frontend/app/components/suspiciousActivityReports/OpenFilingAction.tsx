"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { toast } from "sonner";
import { FileText, Flag } from "lucide-react";

import {
  ProblemDetails,
  SuspiciousActivityReportResource,
  ValidationProblemDetails,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { filingHref } from "@/lib/suspiciousActivityReports";
import { readCaseFiling, useCaseFiling } from "@/lib/useSuspiciousActivityReports";
import { cn } from "@/lib/utils";
import { FilingStatusBadge } from "@/components/suspiciousActivityReports/FilingParts";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function OpenFilingAction({
  caseId,
  className,
}: {
  caseId: string;
  className?: string;
}) {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const { report, loading, refresh } = useCaseFiling(caseId);

  const [open, setOpen] = useState(false);
  const [byHand, setByHand] = useState(false);
  const [narrative, setNarrative] = useState("");
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const create = async () => {
    setCreating(true);
    setError(null);

    const typed = narrative.trim();

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/suspicious-activity-report`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          // The body is omitted entirely for the composed draft. Sending an
          // empty narrative is not the same request: it would open the report
          // with nothing in it, which is the one starting point that helps
          // nobody.
          body: JSON.stringify(byHand && typed.length > 0 ? { narrative: typed } : {}),
        }
      );

      if (response.ok) {
        const created = await unwrap<SuspiciousActivityReportResource>(
          response
        );
        setOpen(false);
        toast.success(t("suspiciousActivityReports.filingOpened"), {
          description:
            t("suspiciousActivityReports.deadlineSetFromCaseS"),
        });
        router.push(
          filingHref(created.suspiciousActivityReport.suspiciousActivityReportId)
        );
        return;
      }

      if (response.status === 409) {
        const problem = (await response.json().catch(() => null)) as
          | ProblemDetails
          | null;
        const explanation =
          problem?.detail ||
          "This case already has a suspicious activity report.";

        setOpen(false);

        const existing = await readCaseFiling(caseId);
        if (existing) {
          toast.info(explanation);
          router.push(
            filingHref(
              existing.suspiciousActivityReport.suspiciousActivityReportId
            )
          );
          return;
        }

        toast.info(explanation, {
          description:
            t("suspiciousActivityReports.couldNotOpenedJustNow"),
        });
        refresh();
        return;
      }

      if (response.status === 400) {
        const problem = (await response.json().catch(() => null)) as
          | ValidationProblemDetails
          | null;
        const messages = problem?.errors
          ? Object.values(problem.errors).flat()
          : [];
        setError(
          messages.length > 0
            ? messages.join("\n")
            : problem?.detail || "The filing was refused."
        );
        return;
      }

      await reportApiError(response, {
        fallback: t("suspiciousActivityReports.filingCouldNotOpened"),
        onUnauthorized: () => router.push(loginHref(pathname)),
      });
    } catch {
      toast.error(t("suspiciousActivityReports.filingCouldNotOpened"));
    } finally {
      setCreating(false);
    }
  };

  if (loading) return null;

  if (report) {
    const existing = report.suspiciousActivityReport;
    return (
      <span className={cn("inline-flex items-center gap-2", className)}>
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href={filingHref(existing.suspiciousActivityReportId)}>
            <FileText className="size-4" />
            {existing.friendlyName}
          </Link>
        </Button>
        <FilingStatusBadge status={existing.status} />
      </span>
    );
  }

  return (
    <>
      <Button
        variant="outline"
        size="sm"
        className={cn("gap-1.5 text-inflight-700 hover:text-inflight-700", className)}
        onClick={() => {
          setByHand(false);
          setNarrative("");
          setError(null);
          setOpen(true);
        }}
      >
        <Flag className="size-4" />
        {t("suspiciousActivityReports.openAFiling")}
      </Button>

      <Dialog
        open={open}
        onOpenChange={(next) => {
          if (!next && !creating) setOpen(false);
        }}
      >
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("suspiciousActivityReports.openASuspiciousActivityReport")}</DialogTitle>
            <DialogDescription>
              This records that somebody has considered this case for reporting
              to the financial intelligence unit. There is one report per case,
              it is never deleted, and its deadline is fixed from the case&apos;s
              opening time — so opening it now does not buy any more time, and
              leaving it unopened does not stop the clock.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-3">
            <Choice
              selected={!byHand}
              title={t("suspiciousActivityReports.startFromTheComposedDraft")}
              body="Builds a first draft out of the case — the payment, why it was flagged, the parties, and what the investigation found. It is a starting point and says so in its own text; you rewrite whatever the case does not actually support before submitting."
              onSelect={() => {
                setByHand(false);
                setError(null);
              }}
            />
            <Choice
              selected={byHand}
              title={t("suspiciousActivityReports.writeTheNarrativeMyself")}
              body="Opens the report with your words instead. You can still compose a draft from the case afterwards, though doing so replaces everything you write here."
              onSelect={() => {
                setByHand(true);
                setError(null);
              }}
            />
          </div>

          {byHand && (
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("suspiciousActivityReports.narrative")}
              </label>
              <Textarea
                value={narrative}
                onChange={(event) => {
                  setNarrative(event.target.value);
                  if (error) setError(null);
                }}
                rows={10}
                placeholder={"SUMMARY\n…"}
                className="whitespace-pre-wrap font-mono text-[13px] leading-relaxed"
              />
              <p className="text-xs text-slate-500">
                Plain text. Leave it empty and the report opens with the composed
                draft instead — an empty narrative is not a starting point.
              </p>
            </div>
          )}

          {error && (
            <p className="whitespace-pre-line text-xs text-critical-700">
              {error}
            </p>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              disabled={creating}
              onClick={() => setOpen(false)}
            >
              {t("common.cancel")}
            </Button>
            <Button disabled={creating} onClick={create}>
              {creating ? "Opening..." : "Open filing"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

function Choice({
  selected,
  title,
  body,
  onSelect,
}: {
  selected: boolean;
  title: string;
  body: string;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={onSelect}
      className={cn(
        "w-full rounded-lg border p-3 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800",
        selected
          ? "border-brand-800 bg-brand-50"
          : "border-slate-200 hover:border-slate-300"
      )}
    >
      <p className="font-mono text-sm font-medium text-slate-900">{title}</p>
      <p className="mt-1 text-sm text-slate-600">{body}</p>
    </button>
  );
}
