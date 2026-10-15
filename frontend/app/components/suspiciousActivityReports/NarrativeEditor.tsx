"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Lock, RefreshCw, Save, Undo2 } from "lucide-react";

import {
  SuspiciousActivityReport,
  SuspiciousActivityReportResource,
  ValidationProblemDetails,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  COMPOSED_NARRATIVE_NOTE,
  isNarrativeEditable,
  narrativeFrozenReason,
} from "@/lib/suspiciousActivityReports";
import { NarrativeText } from "@/components/suspiciousActivityReports/FilingParts";
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

export function NarrativeEditor({
  report,
  onUpdated,
  onConflict,
  onUnauthorized,
}: {
  report: SuspiciousActivityReport;
  onUpdated: (updated: SuspiciousActivityReportResource) => void;
  onConflict: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const editable = isNarrativeEditable(report.status);
  const frozenReason = narrativeFrozenReason(report.status);

  const [draft, setDraft] = useState(report.narrative);
  const [saving, setSaving] = useState(false);
  const [regenerating, setRegenerating] = useState(false);
  const [confirmRegenerate, setConfirmRegenerate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setDraft(report.narrative);
    setError(null);
  }, [report.narrative]);

  const dirty = draft !== report.narrative;

  const write = async (
    method: "PATCH" | "POST",
    path: string,
    body: unknown,
    {
      toastText,
      pendingSetter,
    }: {
      toastText: string;
      pendingSetter: (pending: boolean) => void;
    }
  ) => {
    pendingSetter(true);
    setError(null);

    try {
      const response = await apiFetch(
        `${API_BASE}/suspicious-activity-reports/${report.suspiciousActivityReportId}${path}`,
        {
          method,
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        onUpdated(await unwrap<SuspiciousActivityReportResource>(response));
        toast.success(toastText);
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
            : problem?.detail || "The narrative was refused."
        );
        return;
      }

      if (response.status === 409) {
        const problem = (await response.json().catch(() => null)) as {
          detail?: string;
        } | null;
        toast.info(
          problem?.detail ||
            t("suspiciousActivityReports.filingNoLongerDraftSo")
        );
        onConflict();
        return;
      }

      await reportApiError(response, {
        fallback: t("suspiciousActivityReports.narrativeCouldNotSaved"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("suspiciousActivityReports.narrativeCouldNotSaved"));
    } finally {
      pendingSetter(false);
    }
  };

  const handleSave = () =>
    write(
      "PATCH",
      "",
      { narrative: draft },
      { toastText: "Narrative saved", pendingSetter: setSaving }
    );

  const handleRegenerate = async () => {
    setConfirmRegenerate(false);
    await write(
      "POST",
      "/narrative",
      {},
      {
        toastText: "Draft composed again from the case",
        pendingSetter: setRegenerating,
      }
    );
  };

  return (
    <section className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="font-mono text-sm font-medium tracking-tight text-slate-900">
          {t("suspiciousActivityReports.narrative")}
        </h2>

        {editable && (
          <div className="flex flex-wrap items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              disabled={saving || regenerating}
              onClick={() => setConfirmRegenerate(true)}
            >
              <RefreshCw className="size-4" />
              {t("suspiciousActivityReports.regenerateDraft")}
            </Button>
            {dirty && (
              <Button
                variant="ghost"
                size="sm"
                className="gap-1.5 text-slate-500 hover:text-slate-900"
                disabled={saving || regenerating}
                onClick={() => {
                  setDraft(report.narrative);
                  setError(null);
                }}
              >
                <Undo2 className="size-4" />
                {t("suspiciousActivityReports.discardChanges")}
              </Button>
            )}
            <Button
              size="sm"
              className="gap-1.5"
              disabled={saving || regenerating || !dirty}
              onClick={handleSave}
            >
              <Save className="size-4" />
              {saving ? "Saving..." : "Save narrative"}
            </Button>
          </div>
        )}
      </div>

      {editable ? (
        <>
          <p className="max-w-prose text-sm text-slate-500">
            {COMPOSED_NARRATIVE_NOTE}
          </p>
          <Textarea
            value={draft}
            onChange={(event) => {
              setDraft(event.target.value);
              if (error) setError(null);
            }}
            rows={26}
            spellCheck
            disabled={saving || regenerating}
            aria-invalid={error ? true : undefined}
            aria-label={t("suspiciousActivityReports.filingNarrative")}
            className="min-h-[28rem] whitespace-pre-wrap font-mono text-[13px] leading-relaxed"
          />
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-xs text-slate-500">
              Plain text. The headings and the line breaks are part of the
              report — what you see here is what goes into the goAML file.
            </p>
            {dirty && (
              <p className="font-mono text-xs uppercase tracking-widest text-attention-700">
                {t("suspiciousActivityReports.unsavedChanges")}
              </p>
            )}
          </div>
          {error && (
            <p className="whitespace-pre-line text-xs text-critical-700">
              {error}
            </p>
          )}
        </>
      ) : (
        <>
          {frozenReason && (
            <p className="flex items-start gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
              <Lock className="mt-0.5 size-4 flex-shrink-0 text-slate-400" />
              <span className="max-w-prose">{frozenReason}</span>
            </p>
          )}
          <NarrativeText narrative={report.narrative} />
        </>
      )}

      <Dialog
        open={confirmRegenerate}
        onOpenChange={(open) => {
          if (!open && !regenerating) setConfirmRegenerate(false);
        }}
      >
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("suspiciousActivityReports.composeThisDraftAgainFrom")}</DialogTitle>
            <DialogDescription>
              This throws away every edit anybody has made to the narrative and
              replaces the whole thing with a fresh draft composed from the case.
              Anything written by hand is gone and cannot be recovered from this
              screen.
            </DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            {dirty
              ? "You have unsaved changes as well, and those go too."
              : "The narrative as it stands now will be replaced."}{" "}
            The filing stays in draft, and its deadline does not move.
          </p>

          <DialogFooter>
            <Button
              variant="outline"
              disabled={regenerating}
              onClick={() => setConfirmRegenerate(false)}
            >
              {t("suspiciousActivityReports.keepWhatIsWritten")}
            </Button>
            <Button disabled={regenerating} onClick={handleRegenerate}>
              {regenerating ? "Composing..." : "Discard and compose again"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </section>
  );
}
