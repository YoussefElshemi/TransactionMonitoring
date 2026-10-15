"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { RotateCcw, Save, TriangleAlert } from "lucide-react";

import { SlaTarget } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { cn } from "@/lib/utils";
import {
  SLA_SAVED_NOTE,
  SLA_SAVE_NOTE,
  SlaTargetDraft,
  TARGET_MINUTES_MAX,
  TARGET_MINUTES_MIN,
  TARGET_UNITS,
  TargetUnit,
  UNIT_LABELS,
  draftMinutes,
  draftsFrom,
  houseDefaultFor,
  isDraftValid,
  isSlaDirty,
  slaTargetsBody,
  targetLabel,
} from "@/lib/slaTargets";
import { SeverityBadge } from "@/components/SeverityBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { apiFetch } from "@/lib/http";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function SlaTargetsPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const [drafts, setDrafts] = useState<SlaTargetDraft[]>([]);
  const [loaded, setLoaded] = useState<SlaTargetDraft[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetchTargets = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/sla-targets`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const targets = await unwrap<SlaTarget[]>(response);
        const next = draftsFrom(targets);
        setDrafts(next);
        setLoaded(next);
      } else {
        await reportApiError(response, {
          fallback: t("organization.errorOccurredFetchingSlaTargets"),
          onUnauthorized,
          toastId: "error-fetching-sla-targets",
        });
      }
    } catch {
      toast.error(t("organization.errorOccurredFetchingSlaTargets"), {
        id: "error-fetching-sla-targets",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  useEffect(() => {
    fetchTargets();
  }, [fetchTargets]);

  const update = (severity: string, patch: Partial<SlaTargetDraft>) =>
    setDrafts((current) =>
      current.map((draft) =>
        draft.severity === severity ? { ...draft, ...patch } : draft
      )
    );

  const handleSave = async () => {
    setSaving(true);

    try {
      const body = slaTargetsBody(drafts);

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/sla-targets`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        toast.success(t("organization.sLATargetsSaved"), { description: SLA_SAVED_NOTE });
        await fetchTargets();
      } else {
        await reportApiError(response, {
          fallback: t("organization.failedSaveSlaTargets"),
          onUnauthorized,
          fieldLabels: { "Body.Targets": "Targets" },
        });
      }
    } catch {
      toast.error(t("organization.failedSaveSlaTargets"));
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Card>
        <CardContent className="space-y-3 pt-6">
          {Array.from({ length: 5 }).map((_, row) => (
            <Skeleton key={row} className="h-10 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  const invalid = drafts.some((draft) => !isDraftValid(draft));
  const dirty = isSlaDirty(drafts, loaded);
  const allDefault = drafts.every((draft) => draft.useDefault);

  return (
    <div className="space-y-4">
      <Card>
        <CardContent className="space-y-5 pt-6">
          <div>
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("organization.howLongTheDeskHas")}
            </h2>
            <p className="mt-1 max-w-prose text-sm text-slate-500">
              A case&apos;s deadline is the earlier of its severity&apos;s target
              and the moment the hold underneath it runs out, worked out once
              when the case opens.
            </p>
            <p className="mt-2 max-w-prose text-sm text-slate-500">
              Every band starts on the house default. Type a target to declare
              one; a band you leave alone is left out of the save and goes on
              following the default, including if the default later changes.
            </p>
          </div>

          <div className="divide-y divide-slate-100">
            {drafts.map((draft) => {
              const minutes = draftMinutes(draft);
              const valid = isDraftValid(draft);
              const houseDefault = houseDefaultFor(draft.severity);

              return (
                <div
                  key={draft.severity}
                  className="flex flex-wrap items-center gap-x-4 gap-y-2 py-3"
                >
                  <div className="w-24 flex-shrink-0">
                    <SeverityBadge severity={draft.severity} emphasis />
                  </div>

                  <div className="flex items-center gap-2">
                    <Input
                      value={draft.amount}
                      onChange={(event) =>
                        update(draft.severity, {
                          amount: event.target.value,
                          useDefault: false,
                        })
                      }
                      inputMode="numeric"
                      disabled={saving}
                      className={cn(
                        "w-24 font-mono",
                        !valid && "border-critical-500"
                      )}
                    />
                    <Select
                      value={draft.unit}
                      onValueChange={(next) =>
                        update(draft.severity, {
                          unit: next as TargetUnit,
                          useDefault: false,
                        })
                      }
                      disabled={saving}
                    >
                      <SelectTrigger className="w-32">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {TARGET_UNITS.map((unit) => (
                          <SelectItem key={unit} value={unit}>
                            {UNIT_LABELS[unit]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="min-w-0 flex-1">
                    {!valid ? (
                      <p className="text-xs text-critical-500">
                        Must be a whole number of minutes between{" "}
                        {TARGET_MINUTES_MIN} and{" "}
                        {format.count(TARGET_MINUTES_MAX)} (one year).
                      </p>
                    ) : draft.useDefault ? (
                      <p className="text-xs text-slate-500">
                        Following the house default of{" "}
                        {targetLabel(houseDefault)} — this band is left out of
                        the save and keeps tracking it.
                      </p>
                    ) : (
                      <p className="font-mono text-xs text-slate-500">
                        {format.count(minutes)} minutes
                        {minutes === houseDefault && (
                          <span className="ml-2 font-sans text-slate-400">
                            same as the default, but declared explicitly
                          </span>
                        )}
                      </p>
                    )}
                  </div>

                  {!draft.useDefault && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="gap-1.5 font-mono text-[11px] uppercase tracking-widest text-slate-400 hover:text-slate-900"
                      disabled={saving}
                      onClick={() =>
                        update(draft.severity, {
                          useDefault: true,
                          amount: String(houseDefault),
                          unit: "minutes" as TargetUnit,
                        })
                      }
                      title={t("organization.leaveThisBandOutOf")}
                    >
                      <RotateCcw className="size-3.5" />
                      {t("organization.useDefault")}
                    </Button>
                  )}
                </div>
              );
            })}
          </div>

          {allDefault && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
              Every band is on the house default, so saving sends an empty set —
              which is how an organisation gives back targets it declared
              earlier.
            </p>
          )}
        </CardContent>
      </Card>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <Button
          className="gap-1.5"
          onClick={handleSave}
          disabled={saving || !dirty || invalid}
        >
          <Save className="size-4" />
          {saving ? "Saving..." : "Save targets"}
        </Button>
        {dirty && (
          <Button
            variant="outline"
            disabled={saving}
            onClick={() => setDrafts(loaded)}
          >
            {t("organization.discard")}
          </Button>
        )}
        <p className="flex items-start gap-2 text-sm text-attention-700">
          <TriangleAlert className="mt-0.5 size-4 flex-shrink-0" />
          <span className="max-w-prose">{SLA_SAVE_NOTE}</span>
        </p>
      </div>
    </div>
  );
}
