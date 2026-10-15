"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import {
  AlertTriangle,
  Gauge,
  Plus,
  RotateCcw,
  Trash2,
} from "lucide-react";

import {
  AuditLogChange,
  SchemaField,
  TransactionSchemaResource,
  VelocityBackfill,
  VelocityBackfillResource,
  VelocityMetric,
  VelocityProfile,
  VelocityProfileResource,
  VelocityProfileUsage,
  VelocityThreshold,
} from "@/types";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { HistoryPanel, historyPath } from "@/components/history";
import { SchemaFieldCombobox } from "@/components/SchemaFieldCombobox";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
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
  API_BASE,
  readSchemaViolations,
  reportApiError,
  unwrap,
  unwrapPage,
} from "@/lib/api";
import {
  Breakage,
  DraftMetric,
  DraftThreshold,
  MAX_METRICS,
  MAX_THRESHOLDS,
  MetricField,
  ThresholdField,
  breakagesOf,
  describeBreakage,
  diffNamed,
  hasErrors,
  introducedMetrics,
  metricPaths,
  newDraftMetric,
  newDraftThreshold,
  parseVelocityMetrics,
  parseVelocityThresholds,
  readVelocityUsages,
  sameProfile,
  staleMetrics,
  toMetricDrafts,
  toRequestMetrics,
  toRequestThresholds,
  toThresholdDrafts,
  validateDrafts,
} from "@/lib/velocityProfile";
import { invalidateVelocityProfile } from "@/lib/useVelocityProfile";
import { cn } from "@/lib/utils";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

const SECTIONS = ["Profile", "Backfills", "History"] as const;
type Section = (typeof SECTIONS)[number];

const BACKFILLS_PAGE_SIZE = 10;

const BACKFILL_POLL_MS = 4000;

const validationLabels = (
  metrics: DraftMetric[],
  thresholds: DraftThreshold[]
): Record<string, string> => {
  const labels: Record<string, string> = {
    "Body.Metrics": "Metrics",
    "Body.Thresholds": "Thresholds",
  };

  metrics.forEach((metric, index) => {
    const named = metric.name.trim() || `metric ${index + 1}`;
    labels[`Body.Metrics[${index}]`] = named;
    labels[`Body.Metrics[${index}].Name`] = `${named} name`;
    labels[`Body.Metrics[${index}].Path`] = `${named} path`;
  });

  thresholds.forEach((threshold, index) => {
    const named = threshold.name.trim() || `threshold ${index + 1}`;
    labels[`Body.Thresholds[${index}]`] = named;
    labels[`Body.Thresholds[${index}].Name`] = `${named} name`;
    labels[`Body.Thresholds[${index}].Value`] = `${named} value`;
  });

  return labels;
};

const ROW_KEY = /^Body\.(Metrics|Thresholds)\[(\d+)\](?:\.(\w+))?/;

export function VelocityProfilePanel({
  organizationSegment,
  canEdit = true,
  onUnauthorized,
}: {
  organizationSegment: string;
  canEdit?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const base = `${API_BASE}/organizations/${organizationSegment}/velocity-profile`;

  const [section, setSection] = useState<Section>("Profile");
  const [profile, setProfile] = useState<VelocityProfile | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [failed, setFailed] = useState<boolean>(false);
  const [metricDrafts, setMetricDrafts] = useState<DraftMetric[]>([]);
  const [thresholdDrafts, setThresholdDrafts] = useState<DraftThreshold[]>([]);
  const [saving, setSaving] = useState<boolean>(false);
  const [metricErrors, setMetricErrors] = useState<
    Record<string, Partial<Record<MetricField, string>>>
  >({});
  const [thresholdErrors, setThresholdErrors] = useState<
    Record<string, Partial<Record<ThresholdField, string>>>
  >({});
  const [formError, setFormError] = useState<string | null>(null);
  const [withdrawing, setWithdrawing] = useState<boolean>(false);
  const [restoring, setRestoring] = useState<boolean>(false);
  const [confirmingWithdraw, setConfirmingWithdraw] = useState<boolean>(false);
  const [usages, setUsages] = useState<VelocityProfileUsage[]>([]);
  const [refusedBy, setRefusedBy] = useState<VelocityProfileUsage[] | null>(
    null
  );
  const [backfilling, setBackfilling] = useState<string[] | null>(null);
  const [backfillsKey, setBackfillsKey] = useState<number>(0);
  const [schemaFields, setSchemaFields] = useState<SchemaField[] | null>(null);

  const fetchProfile = useCallback(
    async ({ keepDrafts = false }: { keepDrafts?: boolean } = {}) => {
      setLoading(true);
      try {
        const response = await apiFetch(`${base}?includeDeleted=true`, {
          credentials: "include",
        });

        if (response.ok) {
          const resource = await unwrap<VelocityProfileResource>(response);
          setProfile(resource.velocityProfile);
          setFailed(false);
          if (!keepDrafts) {
            const declared = resource.velocityProfile.isDeleted
              ? null
              : resource.velocityProfile;
            setMetricDrafts(toMetricDrafts(declared?.metrics ?? []));
            setThresholdDrafts(toThresholdDrafts(declared?.thresholds ?? []));
          }
          return;
        }

        if (response.status === 404) {
          setProfile(null);
          setFailed(false);
          if (!keepDrafts) {
            setMetricDrafts([]);
            setThresholdDrafts([]);
          }
          return;
        }

        setFailed(true);
        await reportApiError(response, {
          fallback: t("organization.failedLoadVelocityProfile"),
          onUnauthorized,
          toastId: "error-velocity-profile",
        });
      } catch {
        setFailed(true);
        toast.error(t("organization.failedLoadVelocityProfile"), {
          id: "error-velocity-profile",
        });
      } finally {
        setLoading(false);
      }
    },
    [base, onUnauthorized]
  );

  const fetchUsages = useCallback(async () => {
    try {
      const response = await apiFetch(`${base}/usages`, { credentials: "include" });
      if (!response.ok) {
        setUsages([]);
        return;
      }
      setUsages(await unwrap<VelocityProfileUsage[]>(response));
    } catch {
      setUsages([]);
    }
  }, [base]);

  const fetchSchema = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/transaction-schema`,
        { credentials: "include" }
      );
      if (!response.ok) {
        setSchemaFields(null);
        return;
      }
      const resource = await unwrap<TransactionSchemaResource>(response);
      setSchemaFields(resource.transactionSchema.fields);
    } catch {
      setSchemaFields(null);
    }
  }, [organizationSegment]);

  useEffect(() => {
    fetchProfile();
    fetchUsages();
    fetchSchema();
  }, [fetchProfile, fetchUsages, fetchSchema]);

  const observing = profile !== null && !profile.isDeleted;
  const withdrawn = profile !== null && profile.isDeleted;
  const everExisted = profile !== null;

  const filledMetrics = useMemo(
    () =>
      metricDrafts.filter((draft) => draft.name.trim() || draft.path.trim()),
    [metricDrafts]
  );
  const filledThresholds = useMemo(
    () =>
      thresholdDrafts.filter(
        (draft) => draft.name.trim() || draft.value.trim()
      ),
    [thresholdDrafts]
  );

  const requestMetrics = useMemo(
    () => toRequestMetrics(metricDrafts),
    [metricDrafts]
  );
  const requestThresholds = useMemo(
    () => toRequestThresholds(thresholdDrafts),
    [thresholdDrafts]
  );

  const dirty = !sameProfile(
    { metrics: requestMetrics, thresholds: requestThresholds },
    {
      metrics: observing ? profile.metrics : [],
      thresholds: observing ? (profile.thresholds ?? []) : [],
    }
  );

  const breakages = useMemo(
    () => breakagesOf(requestMetrics, requestThresholds, usages),
    [requestMetrics, requestThresholds, usages]
  );

  const stale = useMemo(
    () => staleMetrics(observing ? profile.metrics : null, schemaFields),
    [observing, profile, schemaFields]
  );

  const wouldBackfill = useMemo(
    () => introducedMetrics(requestMetrics, observing ? profile.metrics : null),
    [requestMetrics, observing, profile]
  );

  const touched = () => {
    setFormError(null);
    setRefusedBy(null);
  };

  const updateMetric = (id: string, updates: Partial<DraftMetric>) => {
    touched();
    setMetricDrafts((prev) =>
      prev.map((draft) => (draft.id === id ? { ...draft, ...updates } : draft))
    );
    setMetricErrors((prev) => {
      if (!prev[id]) return prev;
      const next = { ...prev };
      delete next[id];
      return next;
    });
  };

  const updateThreshold = (id: string, updates: Partial<DraftThreshold>) => {
    touched();
    setThresholdDrafts((prev) =>
      prev.map((draft) => (draft.id === id ? { ...draft, ...updates } : draft))
    );
    setThresholdErrors((prev) => {
      if (!prev[id]) return prev;
      const next = { ...prev };
      delete next[id];
      return next;
    });
  };

  const reset = () => {
    setMetricDrafts(toMetricDrafts(observing ? profile.metrics : []));
    setThresholdDrafts(
      toThresholdDrafts(observing ? (profile.thresholds ?? []) : [])
    );
    setMetricErrors({});
    setThresholdErrors({});
    setFormError(null);
    setRefusedBy(null);
  };

  const markViolations = (
    violations: { field: string; message: string }[]
  ): string[] => {
    const unplaced: string[] = [];
    const next: Record<string, Partial<Record<MetricField, string>>> = {};

    for (const violation of violations) {
      const byPath = metricDrafts.find(
        (draft) => draft.path.trim() === violation.field
      );
      const byName = metricDrafts.find(
        (draft) =>
          draft.name.trim().toLowerCase() === violation.field.toLowerCase()
      );
      const row = byPath ?? byName;

      if (!row) {
        unplaced.push(`${violation.field}: ${violation.message}`);
        continue;
      }

      next[row.id] ??= {};
      next[row.id].path = `${violation.field} ${violation.message}`;
    }

    setMetricErrors((prev) => ({ ...prev, ...next }));
    return unplaced;
  };

  const save = async () => {
    const validation = validateDrafts(
      metricDrafts,
      thresholdDrafts,
      schemaFields
    );
    setMetricErrors(validation.metricErrors);
    setThresholdErrors(validation.thresholdErrors);
    setFormError(validation.formError);

    if (hasErrors(validation)) {
      toast.error(
        validation.formError ?? t("organization.someRowsNeedFixingBefore")
      );
      return;
    }

    setSaving(true);
    setRefusedBy(null);
    const introducing = wouldBackfill;

    try {
      const response = await apiFetch(base, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          metrics: requestMetrics,
          // Sent as null rather than as `[]` when there are none: the field is
          // nullable, and an organisation writing no structuring rules declares
          // no thresholds rather than declaring an empty set of them.
          thresholds: requestThresholds.length ? requestThresholds : null,
        }),
        credentials: "include",
      });

      if (response.ok) {
        const resource = await unwrap<VelocityProfileResource>(response);
        setProfile(resource.velocityProfile);
        setMetricDrafts(toMetricDrafts(resource.velocityProfile.metrics));
        setThresholdDrafts(
          toThresholdDrafts(resource.velocityProfile.thresholds ?? [])
        );
        setMetricErrors({});
        setThresholdErrors({});
        invalidateVelocityProfile();
        void fetchUsages();
        setBackfillsKey((key) => key + 1);

        if (introducing.length > 0) {
          setBackfilling(introducing);
          setSection("Backfills");
          toast.success(t("organization.velocityProfileSavedBackfillHas"), {
            description: t(
              introducing.length === 1
                ? "organization.backfillRunningOne"
                : "organization.backfillRunningMany",
              introducing.join(", ")
            ),
          });
        } else {
          setBackfilling(null);
          toast.success(t("organization.velocityProfileSaved"));
        }
        return;
      }

      if (response.status === 409) {
        const body = await response
          .clone()
          .json()
          .catch(() => null);

        const named = readVelocityUsages(body);
        if (named) {
          setRefusedBy(named);
          void fetchUsages();
          toast.error(t("organization.changeWouldBreakRulesRead2"), {
            description:
              t("organization.nothingSavedRulesWouldStrand2"),
          });
          return;
        }

        await fetchProfile({ keepDrafts: true });
        toast.error(t("organization.profileCouldNotSavedStands"), {
          description:
            t("organization.yourChangesHaveNotBeen2"),
        });
        return;
      }

      if (response.status === 400) {
        const body = (await response
          .clone()
          .json()
          .catch(() => null)) as {
          errors?: Record<string, string[]>;
        } | null;

        const schema = readSchemaViolations(body);
        if (schema) {
          const unplaced = markViolations(schema.violations);
          toast.error(schema.title, {
            description:
              unplaced.length > 0
                ? unplaced.join("\n")
                : t("organization.metricsNamedAreMarkedBelow"),
            descriptionClassName: "whitespace-pre-line",
          });
          return;
        }

        if (body?.errors) {
          const nextMetrics: Record<
            string,
            Partial<Record<MetricField, string>>
          > = {};
          const nextThresholds: Record<
            string,
            Partial<Record<ThresholdField, string>>
          > = {};

          for (const [key, messages] of Object.entries(body.errors)) {
            const match = ROW_KEY.exec(key);
            if (!match) continue;
            const message = messages.join(" ");

            if (match[1] === "Metrics") {
              const draft = filledMetrics[Number(match[2])];
              if (!draft) continue;
              const field: MetricField = match[3] === "Name" ? "name" : "path";
              nextMetrics[draft.id] ??= {};
              nextMetrics[draft.id][field] = message;
            } else {
              const draft = filledThresholds[Number(match[2])];
              if (!draft) continue;
              const field: ThresholdField =
                match[3] === "Name" ? "name" : "value";
              nextThresholds[draft.id] ??= {};
              nextThresholds[draft.id][field] = message;
            }
          }

          setMetricErrors(nextMetrics);
          setThresholdErrors(nextThresholds);
        }
      }

      await reportApiError(response, {
        fallback: t("organization.failedSaveVelocityProfile"),
        onUnauthorized,
        fieldLabels: validationLabels(filledMetrics, filledThresholds),
      });
    } catch {
      toast.error(t("organization.failedSaveVelocityProfile"));
    } finally {
      setSaving(false);
    }
  };

  const reportStaleState = async (message: string) => {
    toast.error(message, {
      description: t("organization.reloadingCurrentStateProfile"),
    });
    await fetchProfile();
  };

  const withdraw = async () => {
    setWithdrawing(true);

    try {
      const response = await apiFetch(base, {
        method: "DELETE",
        credentials: "include",
      });

      if (response.ok) {
        setConfirmingWithdraw(false);
        setMetricDrafts([]);
        setThresholdDrafts([]);
        setMetricErrors({});
        setThresholdErrors({});
        setFormError(null);
        await fetchProfile();
        invalidateVelocityProfile();
        toast.success(t("organization.organisationNoLongerRecordingObservations"), {
          description:
            t("organization.observationsAlreadyTakenStayExactly"),
        });
        return;
      }

      if (response.status === 409) {
        const body = await response
          .clone()
          .json()
          .catch(() => null);
        const named = readVelocityUsages(body);
        if (named) {
          setRefusedBy(named);
          setConfirmingWithdraw(false);
          setSection("Profile");
          void fetchUsages();
          toast.error(t("organization.rulesStillReadObservationStore"), {
            description:
              t("organization.profileUnchangedChangeDeleteRules"),
          });
          return;
        }
      }

      if (response.status === 404) {
        setConfirmingWithdraw(false);
        await reportStaleState("There is no profile in force to withdraw");
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedWithdrawVelocityProfile"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedWithdrawVelocityProfile"));
    } finally {
      setWithdrawing(false);
    }
  };

  const restore = async () => {
    setRestoring(true);

    try {
      const response = await apiFetch(`${base}/recover`, {
        method: "POST",
        credentials: "include",
      });

      if (response.ok) {
        await fetchProfile();
        invalidateVelocityProfile();
        void fetchUsages();
        setBackfillsKey((key) => key + 1);
        toast.success(t("organization.velocityProfileRestored"), {
          description:
            t("organization.observationsAreRecordedAgainFrom"),
        });
        return;
      }

      if (response.status === 400) {
        await reportStaleState("This profile is already in force");
        return;
      }

      if (response.status === 404) {
        await reportStaleState("There is no withdrawn profile to restore");
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedRestoreVelocityProfile"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedRestoreVelocityProfile"));
    } finally {
      setRestoring(false);
    }
  };

  const readOnly = !canEdit || saving;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-1.5">
        {SECTIONS.filter(
          (candidate) => candidate !== "History" || everExisted
        ).map((candidate) => (
          <Button
            key={candidate}
            type="button"
            size="sm"
            variant={section === candidate ? "default" : "outline"}
            onClick={() => setSection(candidate)}
          >
            {candidate}
          </Button>
        ))}
        {!loading && !failed && (
          <div className="ml-auto flex items-center gap-2">
            <StatusBadge
              status={observing ? "Enabled" : "Disabled"}
              label={observing ? t("organization.recordingObservations") : t("organization.notRecording")}
            />
            {observing && (
              <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {profile.metrics.length}{" "}
                {profile.metrics.length === 1 ? "metric" : "metrics"}
              </span>
            )}
          </div>
        )}
      </div>

      {section === "Profile" && (
        <Card>
          <CardContent className="space-y-4 pt-6">
            <div>
              <h2 className="text-sm font-semibold text-slate-900">
                {t("organization.velocityProfile")}
              </h2>
              <p className="mt-0.5 text-sm text-slate-500">
                What the engine remembers between payments. Declare a metric and
                every payment from then on records one figure for it, per party,
                per role — which is what lets a rule ask a question no single
                payload can answer: how much this remitter has sent today, how
                many different beneficiaries they paid in an hour, how often
                they stopped just short of a reporting threshold.
              </p>
            </div>

            {usages.length > 0 && <UsageBanner usages={usages} />}

            {stale.length > 0 && <StaleMetricsNotice metrics={stale} />}

            {loading ? (
              <div className="space-y-2">
                {Array.from({ length: 2 }).map((_, index) => (
                  <Skeleton key={index} className="h-24 w-full" />
                ))}
              </div>
            ) : failed ? (
              <div className="rounded-md border border-dashed border-slate-200 py-12 text-center">
                <p className="text-sm text-slate-500">
                  {t("organization.theVelocityProfileCouldNot")}
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-3"
                  onClick={() => fetchProfile()}
                >
                  {t("common.retry")}
                </Button>
              </div>
            ) : (
              <>
                {!observing && (
                  <NotRecordingNotice
                    withdrawn={withdrawn ? profile : null}
                    canEdit={canEdit}
                    restoring={restoring}
                    onRestore={restore}
                  />
                )}

                {refusedBy && <RefusalNotice usages={refusedBy} />}

                {breakages.length > 0 && !refusedBy && (
                  <BreakageNotice breakages={breakages} />
                )}

                <MetricsEditor
                  drafts={metricDrafts}
                  errors={metricErrors}
                  schemaFields={schemaFields}
                  readOnly={readOnly}
                  onChange={updateMetric}
                  onAdd={() => {
                    touched();
                    setMetricDrafts((prev) => [...prev, newDraftMetric()]);
                  }}
                  onRemove={(id) => {
                    touched();
                    setMetricDrafts((prev) =>
                      prev.filter((draft) => draft.id !== id)
                    );
                  }}
                />

                <ThresholdsEditor
                  drafts={thresholdDrafts}
                  errors={thresholdErrors}
                  readOnly={readOnly}
                  onChange={updateThreshold}
                  onAdd={() => {
                    touched();
                    setThresholdDrafts((prev) => [
                      ...prev,
                      newDraftThreshold(),
                    ]);
                  }}
                  onRemove={(id) => {
                    touched();
                    setThresholdDrafts((prev) =>
                      prev.filter((draft) => draft.id !== id)
                    );
                  }}
                />

                {formError && (
                  <p className="text-xs text-critical-700">{formError}</p>
                )}

                {dirty && wouldBackfill.length > 0 && (
                  <div className="rounded-md border border-brand-200 bg-brand-50 px-3 py-2 text-xs text-brand-900">
                    Saving will start a backfill for{" "}
                    <span className="font-mono">
                      {wouldBackfill.join(", ")}
                    </span>
                    . A metric declared today would otherwise be blind until
                    tomorrow&apos;s payments arrived, and a rule written against
                    it would read as live while matching nothing — so the
                    payments already stored are caught up first.
                  </div>
                )}

                {canEdit && (
                  <div className="flex flex-wrap items-center gap-2 border-t border-slate-100 pt-4">
                    <Button size="sm" onClick={save} disabled={saving || !dirty}>
                      {saving
                        ? "Saving..."
                        : `Save profile (${requestMetrics.length} ${
                            requestMetrics.length === 1 ? "metric" : "metrics"
                          })`}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={reset}
                      disabled={saving || !dirty}
                    >
                      {t("common.reset")}
                    </Button>
                    {observing && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="ml-auto text-critical-500 hover:text-critical-700"
                        disabled={saving || withdrawing}
                        onClick={() => setConfirmingWithdraw(true)}
                      >
                        {t("organization.stopRecording")}
                      </Button>
                    )}
                  </div>
                )}

                <p className="text-xs text-slate-400">
                  Saving replaces the declaration outright. A metric dropped here
                  stops being recorded from the next evaluate on, and the
                  observations already taken for it are kept — they are what the
                  payments said.
                </p>
              </>
            )}
          </CardContent>
        </Card>
      )}

      {section === "Backfills" && (
        <BackfillsSection
          base={base}
          refreshKey={backfillsKey}
          highlighted={backfilling}
          onUnauthorized={onUnauthorized}
        />
      )}

      {section === "History" && everExisted && (
        <VelocityProfileHistory
          organizationSegment={organizationSegment}
          onUnauthorized={onUnauthorized}
        />
      )}

      <WithdrawDialog
        open={confirmingWithdraw}
        withdrawing={withdrawing}
        metricCount={profile?.metrics.length ?? 0}
        usages={usages}
        discardsEdits={dirty}
        onOpenChange={setConfirmingWithdraw}
        onConfirm={withdraw}
      />
    </div>
  );
}

function MetricsEditor({
  drafts,
  errors,
  schemaFields,
  readOnly,
  onChange,
  onAdd,
  onRemove,
}: {
  drafts: DraftMetric[];
  errors: Record<string, Partial<Record<MetricField, string>>>;
  schemaFields: SchemaField[] | null;
  readOnly: boolean;
  onChange: (id: string, updates: Partial<DraftMetric>) => void;
  onAdd: () => void;
  onRemove: (id: string) => void;
}) {
  const t = useTranslation();
  const options = useMemo(() => metricPaths(schemaFields), [schemaFields]);

  return (
    <div className="rounded-lg border border-slate-200 bg-card p-3">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-sm font-medium text-slate-900">{t("organization.metrics")}</h3>
        <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {drafts.length} of {MAX_METRICS}
        </span>
        {!readOnly && drafts.length < MAX_METRICS && (
          <Button
            variant="outline"
            size="sm"
            className="ml-auto h-7 gap-1 text-xs"
            onClick={onAdd}
          >
            <Plus className="size-3" />
            {t("organization.addMetric")}
          </Button>
        )}
      </div>

      <p className="mt-1.5 text-xs text-slate-400">
        One figure per payment, per party, per role. Give it a name — that is
        what a rule addresses it by — and say where in the payload it is read
        from: a string to count identities of, a number to total, or a date. Not
        a true/false field, which has nothing to measure.
      </p>

      {drafts.length === 0 ? (
        <p className="mt-3 rounded-md border border-dashed border-slate-300 bg-slate-50/60 px-4 py-8 text-center text-xs text-slate-500">
          Nothing is being remembered. Add a metric — an amount, a beneficiary
          account, a country — and every payment from then on records it against
          the parties it names.
        </p>
      ) : (
        <div className="mt-3 space-y-2">
          {drafts.map((draft) => {
            const error = errors[draft.id];
            return (
              <div key={draft.id} className="space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Input
                    placeholder={t("common.name")}
                    value={draft.name}
                    disabled={readOnly}
                    onChange={(event) =>
                      onChange(draft.id, { name: event.target.value })
                    }
                    title={draft.name}
                    className={cn(
                      "min-w-[11rem] max-w-[20rem] flex-1 font-mono",
                      error?.name && "border-critical-500"
                    )}
                  />
                  <span aria-hidden className="font-mono text-xs text-slate-300">
                    ←
                  </span>
                  <SchemaFieldCombobox
                    value={draft.path}
                    onChange={(path) => onChange(draft.id, { path })}
                    disabled={readOnly}
                    fields={schemaFields}
                    paths={options}
                    placeholder={t("organization.amount")}
                    tone={error?.path ? "critical" : "default"}
                    className="min-w-[14rem] max-w-[28rem] flex-1"
                  />
                  {!readOnly && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-critical-500 hover:text-critical-700"
                      onClick={() => onRemove(draft.id)}
                      title={t("organization.removeMetric")}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  )}
                </div>
                {(error?.name || error?.path) && (
                  <p className="text-xs text-critical-700">
                    {error.name ?? error.path}
                  </p>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

function ThresholdsEditor({
  drafts,
  errors,
  readOnly,
  onChange,
  onAdd,
  onRemove,
}: {
  drafts: DraftThreshold[];
  errors: Record<string, Partial<Record<ThresholdField, string>>>;
  readOnly: boolean;
  onChange: (id: string, updates: Partial<DraftThreshold>) => void;
  onAdd: () => void;
  onRemove: (id: string) => void;
}) {
  const t = useTranslation();
  return (
    <div className="rounded-lg border border-slate-200 bg-card p-3">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-sm font-medium text-slate-900">{t("organization.thresholds")}</h3>
        <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {drafts.length} of {MAX_THRESHOLDS}
        </span>
        {!readOnly && drafts.length < MAX_THRESHOLDS && (
          <Button
            variant="outline"
            size="sm"
            className="ml-auto h-7 gap-1 text-xs"
            onClick={onAdd}
          >
            <Plus className="size-3" />
            {t("organization.addThreshold")}
          </Button>
        )}
      </div>

      <p className="mt-1.5 max-w-3xl text-xs text-slate-400">
        A figure your organisation is bound by — the amount a reporting duty
        starts at, typically. Read by a structuring count and by nothing else:
        it asks how many payments sat <em>just under</em> this figure. It lives
        here rather than on the rule because it is a fact, and every rule naming
        it means the same figure. How close counts as &ldquo;just under&rdquo;
        is a judgement, and that stays on each rule as its band.
      </p>

      {drafts.length === 0 ? (
        <p className="mt-3 text-xs text-slate-400">
          None declared. Nothing else needs one — every function except a
          structuring count works without any.
        </p>
      ) : (
        <div className="mt-3 space-y-2">
          {drafts.map((draft) => {
            const error = errors[draft.id];
            return (
              <div key={draft.id} className="space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Input
                    placeholder={t("common.name")}
                    value={draft.name}
                    disabled={readOnly}
                    onChange={(event) =>
                      onChange(draft.id, { name: event.target.value })
                    }
                    title={draft.name}
                    className={cn(
                      "min-w-[11rem] max-w-[20rem] flex-1 font-mono",
                      error?.name && "border-critical-500"
                    )}
                  />
                  <span aria-hidden className="font-mono text-xs text-slate-300">
                    =
                  </span>
                  <Input
                    placeholder="10000"
                    inputMode="decimal"
                    value={draft.value}
                    disabled={readOnly}
                    onChange={(event) =>
                      onChange(draft.id, { value: event.target.value })
                    }
                    className={cn(
                      "w-40 font-mono",
                      error?.value && "border-critical-500"
                    )}
                  />
                  {!readOnly && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-critical-500 hover:text-critical-700"
                      onClick={() => onRemove(draft.id)}
                      title={t("organization.removeThreshold")}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  )}
                </div>
                {(error?.name || error?.value) && (
                  <p className="text-xs text-critical-700">
                    {error.name ?? error.value}
                  </p>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

function UsageLink({ usage }: { usage: VelocityProfileUsage }) {
  const names = [...(usage.metrics ?? []), ...(usage.thresholds ?? [])];
  return (
    <Link
      href={`/rules/${usage.ruleId}`}
      className="inline-flex items-baseline gap-1.5 underline underline-offset-2 hover:no-underline"
    >
      <span className="font-medium">{usage.friendlyName}</span>
      {names.length > 0 && (
        <span className="font-mono text-[11px] uppercase tracking-wide opacity-70">
          {names.join(", ")}
        </span>
      )}
    </Link>
  );
}

function UsageBanner({ usages }: { usages: VelocityProfileUsage[] }) {
  const t = useTranslation();
  const format = useFormat();
  const [open, setOpen] = useState(false);
  const enabled = usages.filter((usage) => usage.isEnabled !== false).length;

  return (
    <div className="rounded-lg border border-brand-200 bg-brand-50 px-3 py-2">
      <div className="flex flex-wrap items-center gap-2">
        <Gauge className="size-4 shrink-0 text-brand-800" />
        <p className="text-sm text-brand-900">
          <span className="font-medium">
            {t(
              usages.length === 1
                ? "organization.velocityUsageOne"
                : "organization.velocityUsageMany",
              format.count(usages.length)
            )}
          </span>
          {enabled !== usages.length && (
            <span className="text-brand-800">
              {t("organization.velocityUsageEnabled", format.count(enabled))}
            </span>
          )}
          {t("organization.velocityUsageRefused")}
        </p>
        <Button
          variant="ghost"
          size="sm"
          className="ml-auto h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
          onClick={() => setOpen((current) => !current)}
        >
          {open ? "Hide" : "Show them"}
        </Button>
      </div>

      {open && (
        <ul className="mt-2 space-y-1 border-t border-brand-200 pt-2 text-sm text-brand-900">
          {usages.map((usage) => (
            <li
              key={usage.ruleId}
              className="flex flex-wrap items-baseline gap-2"
            >
              <UsageLink usage={usage} />
              {usage.isEnabled === false && (
                <StatusBadge status="Disabled" label={t("common.disabled")} />
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function StaleMetricsNotice({ metrics }: { metrics: VelocityMetric[] }) {
  return (
    <div className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2">
      <AlertTriangle className="mt-0.5 size-4 shrink-0 text-attention-700" />
      <div className="space-y-1 text-sm text-attention-700">
        <p className="font-medium">
          {metrics.length === 1
            ? "A metric is reading a path the schema no longer declares"
            : `${metrics.length} metrics are reading paths the schema no longer declares`}
        </p>
        <ul className="space-y-0.5 text-xs">
          {metrics.map((metric) => (
            <li key={metric.name}>
              <span className="font-mono font-medium">{metric.name}</span>{" "}
              <span className="font-mono opacity-70">← {metric.path}</span>
            </li>
          ))}
        </ul>
        <p className="text-xs">
          A metric path is checked when it is declared and not afterwards, so
          this saved cleanly and then stopped finding anything when the schema
          changed. Nothing is being recorded for{" "}
          {metrics.length === 1 ? "it" : "them"}: a count over{" "}
          {metrics.length === 1 ? "it" : "them"} answers zero for ever and a sum
          answers nothing at all, which is indistinguishable from a party who
          has been quiet. Point{" "}
          {metrics.length === 1 ? "it" : "each of them"} at a declared field, or
          declare the field again on the transaction schema.
        </p>
      </div>
    </div>
  );
}

function BreakageNotice({ breakages }: { breakages: Breakage[] }) {
  const t = useTranslation();
  return (
    <div className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2">
      <AlertTriangle className="mt-0.5 size-4 shrink-0 text-attention-700" />
      <div className="space-y-1 text-sm text-attention-700">
        <p className="font-medium">
          {t("organization.thisSaveWillBeRefused")}
        </p>
        <ul className="space-y-0.5 text-xs">
          {breakages.map((breakage, index) => (
            <li key={`${breakage.usage.ruleId}-${breakage.name}-${index}`}>
              <UsageLink usage={breakage.usage} /> {describeBreakage(breakage)}.
            </li>
          ))}
        </ul>
        <p className="text-xs">
          A rule naming a metric the profile does not declare is refused, so the
          profile cannot be edited into a state that would not have supported
          one. Change or delete those rules first.
        </p>
      </div>
    </div>
  );
}

function RefusalNotice({ usages }: { usages: VelocityProfileUsage[] }) {
  const t = useTranslation();
  return (
    <div className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 px-3 py-2">
      <AlertTriangle className="mt-0.5 size-4 shrink-0 text-critical-700" />
      <div className="space-y-1 text-sm text-critical-700">
        <p className="font-medium">{t("organization.nothingWasSaved")}</p>
        <p className="text-xs">
          {t("organization.theseRulesReadSomethingThis")}
        </p>
        <ul className="space-y-0.5 text-xs">
          {usages.map((usage) => (
            <li key={usage.ruleId}>
              <UsageLink usage={usage} />
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function NotRecordingNotice({
  canEdit,
  withdrawn,
  restoring,
  onRestore,
}: {
  canEdit: boolean;
  withdrawn: VelocityProfile | null;
  restoring: boolean;
  onRestore: () => void;
}) {
  const [showProfile, setShowProfile] = useState<boolean>(false);

  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-4 py-3">
      <div>
        <p className="text-sm font-medium text-slate-600">
          {withdrawn
            ? "Observations are not being recorded"
            : "No velocity profile yet"}
        </p>
        <p className="mt-1 max-w-2xl text-xs text-slate-500">
          {withdrawn ? (
            <>
              It was withdrawn
              {withdrawn.deletedAt && (
                <> on <LocalTime value={withdrawn.deletedAt} /></>
              )}
              , so nothing has been recorded since. Everything observed before
              that is still there and still readable by a window. What is
              missing is the period since: a window spanning it is short of the
              payments taken during it, and restoring the declaration does not
              put them back — re-declaring a metric is what backfills them.
            </>
          ) : (
            <>
              Nothing is remembered between payments, and that is not an error —
              a rule can only look at the payment in front of it. Declare a
              metric below and each one starts being recorded per party, which
              is what makes a total, a distinct count or a structuring test
              expressible at all.
            </>
          )}
        </p>

        {canEdit && withdrawn && (
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <Button
              size="sm"
              className="gap-1.5"
              disabled={restoring}
              onClick={onRestore}
            >
              <RotateCcw className="size-4" />
              {restoring ? "Restoring..." : "Restore previous profile"}
            </Button>
            <span className="text-xs text-slate-400">
              — recording resumes with the same metrics, and the gap stays a
              gap.
            </span>
          </div>
        )}

        {withdrawn && (
          <Button
            variant="ghost"
            size="sm"
            className="mt-2 h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
            onClick={() => setShowProfile((open) => !open)}
          >
            {showProfile
              ? "Hide the withdrawn profile"
              : `Show the ${withdrawn.metrics.length} withdrawn ${
                  withdrawn.metrics.length === 1 ? "metric" : "metrics"
                }`}
          </Button>
        )}
      </div>

      {withdrawn && showProfile && (
        <div className="mt-4 text-left">
          <VelocityProfileTable
            metrics={withdrawn.metrics}
            thresholds={withdrawn.thresholds ?? []}
          />
        </div>
      )}
    </div>
  );
}

function WithdrawDialog({
  open,
  withdrawing,
  metricCount,
  usages,
  discardsEdits,
  onOpenChange,
  onConfirm,
}: {
  open: boolean;
  withdrawing: boolean;
  metricCount: number;
  usages: VelocityProfileUsage[];
  discardsEdits: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
}) {
  const t = useTranslation();
  const blocked = usages.length > 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("organization.stopRecordingObservations")}</DialogTitle>
        </DialogHeader>

        <div className="space-y-3 text-sm text-slate-600">
          {blocked && (
            <div className="rounded-md border border-critical-200 bg-critical-50 px-3 py-2 text-xs text-critical-700">
              <p className="font-medium">
                This will be refused: {usages.length}{" "}
                {usages.length === 1 ? "rule reads" : "rules read"} the store.
              </p>
              <ul className="mt-1 space-y-0.5">
                {usages.map((usage) => (
                  <li key={usage.ruleId}>
                    <UsageLink usage={usage} />
                  </li>
                ))}
              </ul>
              <p className="mt-1">
                Change or delete them first. A rule left aggregating a metric
                nothing records would not fail loudly — a count would simply
                answer zero for ever, and a sum would answer nothing at all.
              </p>
            </div>
          )}

          <p>
            New evaluations stop writing observations. Payloads are accepted
            exactly as they are now, and nothing about them is rejected for it.
          </p>
          <ul className="space-y-2">
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.theObservationsAlreadyTakenAre")}
                </span>{" "}
                Every figure recorded so far stays exactly where it is. They are
                what the payments said, and withdrawing does not unsay them.
              </span>
            </li>
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.theDeclarationIsKept")}
                </span>{" "}
                All {metricCount} {metricCount === 1 ? "metric" : "metrics"} stay
                stored, and restoring puts them back exactly as they are.
              </span>
            </li>
            <li className="flex gap-2">
              <span aria-hidden className="text-attention-700">
                !
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.aGapOpensFromNow")}
                </span>{" "}
                Nothing is recorded in between, so any window spanning that
                period will be short of the payments taken in it. Re-declaring a
                metric afterwards backfills the gap; simply restoring does not.
              </span>
            </li>
          </ul>
          {discardsEdits && (
            <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
              The unsaved changes in the form will be discarded — what is kept
              and restorable is the profile as last saved.
            </p>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={withdrawing}
          >
            {t("common.cancel")}
          </Button>
          <Button onClick={onConfirm} disabled={withdrawing}>
            {withdrawing ? "Stopping..." : "Stop recording"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function VelocityProfileTable({
  metrics,
  thresholds,
}: {
  metrics: VelocityMetric[];
  thresholds: VelocityThreshold[];
}) {
  const t = useTranslation();
  if (metrics.length === 0) {
    return <p className="text-sm text-slate-400">{t("organization.noMetricsDeclared")}</p>;
  }

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-lg border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-48">{t("organization.metric")}</TableHead>
              <TableHead>{t("organization.readFrom")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {metrics.map((metric) => (
              <TableRow key={metric.name} className="hover:bg-slate-50/50">
                <TableCell className="font-mono text-sm text-slate-900">
                  {metric.name}
                </TableCell>
                <TableCell className="font-mono text-xs text-slate-600">
                  {metric.path}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {thresholds.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow className="bg-slate-50 hover:bg-slate-50">
                <TableHead className="w-48">{t("organization.threshold")}</TableHead>
                <TableHead>{t("organization.value")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {thresholds.map((threshold) => (
                <TableRow
                  key={threshold.name}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="font-mono text-sm text-slate-900">
                    {threshold.name}
                  </TableCell>
                  <TableCell className="font-mono text-xs text-slate-600 tabular-nums">
                    {threshold.value}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}

function BackfillsSection({
  base,
  refreshKey,
  highlighted,
  onUnauthorized,
}: {
  base: string;
  refreshKey: number;
  highlighted: string[] | null;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [backfills, setBackfills] = useState<VelocityBackfill[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [tick, setTick] = useState<number>(0);
  const active = useRef<boolean>(false);

  useEffect(() => {
    let cancelled = false;

    const fetchBackfills = async () => {
      setLoading((current) => current && backfills.length === 0);
      try {
        const params = new URLSearchParams({
          pageNumber: String(pageNumber),
          pageSize: String(BACKFILLS_PAGE_SIZE),
        });
        const response = await apiFetch(`${base}/backfills?${params}`, {
          credentials: "include",
        });

        if (cancelled) return;

        if (response.ok) {
          const page = await unwrapPage<VelocityBackfillResource>(response);
          const rows = page.data.map((r) => r.velocityBackfill);
          setBackfills(rows);
          setTotalPages(page.metadata.pageCount || 1);
          active.current = rows.some(
            (row) => row.status === "Pending" || row.status === "Running"
          );
          return;
        }

        if (response.status === 404) {
          setBackfills([]);
          setTotalPages(1);
          active.current = false;
          return;
        }

        active.current = false;
        await reportApiError(response, {
          fallback: t("organization.failedLoadBackfills"),
          onUnauthorized,
          toastId: "error-velocity-backfills",
        });
      } catch {
        if (!cancelled) {
          active.current = false;
          toast.error(t("organization.failedLoadBackfills"), {
            id: "error-velocity-backfills",
          });
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchBackfills();
    return () => {
      cancelled = true;
    };
    // `backfills.length` is deliberately not a dependency — it is read only to
    // decide whether to show skeletons, and depending on it would re-fetch on
    // every arrival.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [base, pageNumber, refreshKey, tick, onUnauthorized]);

  useEffect(() => {
    if (!active.current) return;
    const timer = setTimeout(() => setTick((value) => value + 1), BACKFILL_POLL_MS);
    return () => clearTimeout(timer);
  }, [tick, backfills]);

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">{t("organization.backfills")}</h2>
          <p className="mt-0.5 text-sm text-slate-500">
            When a save introduces a metric, the payments already stored are
            caught up so a rule written against it is not reading a history that
            begins today. Only the metrics that save introduced are backfilled —
            one already declared has been recorded on every payment since.
          </p>
        </div>

        {highlighted && highlighted.length > 0 && (
          <div className="rounded-md border border-brand-200 bg-brand-50 px-3 py-2 text-xs text-brand-900">
            A backfill was queued for{" "}
            <span className="font-mono">{highlighted.join(", ")}</span>. Until it
            reads <span className="font-medium">{t("organization.completed")}</span>, any rule
            aggregating {highlighted.length === 1 ? "it" : "them"} is working
            from a history still being assembled.
          </div>
        )}

        {loading ? (
          <div className="space-y-2">
            {Array.from({ length: 3 }).map((_, index) => (
              <Skeleton key={index} className="h-16 w-full" />
            ))}
          </div>
        ) : backfills.length === 0 ? (
          <p className="py-10 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("organization.noBackfillsYet")}
          </p>
        ) : (
          <div className="space-y-2">
            {backfills.map((backfill) => (
              <BackfillRow
                key={backfill.velocityBackfillId}
                backfill={backfill}
              />
            ))}
          </div>
        )}

        {backfills.length > 0 && totalPages > 1 && (
          <Pagination
            handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
            pageNumber={pageNumber}
            totalPages={totalPages}
            handleNextPage={() =>
              setPageNumber((p) => Math.min(totalPages, p + 1))
            }
          />
        )}
      </CardContent>
    </Card>
  );
}

function BackfillRow({ backfill }: { backfill: VelocityBackfill }) {
  const t = useTranslation();
  const format = useFormat();
  const total = backfill.totalTransactions;
  const done = backfill.processedCount;
  const fraction = total > 0 ? Math.min(1, done / total) : 1;
  const failed = backfill.status === "Failed";
  const running = backfill.status === "Running" || backfill.status === "Pending";

  return (
    <div
      className={cn(
        "rounded-lg border bg-card px-3 py-2",
        failed ? "border-critical-200 ring-1 ring-critical-200" : "border-slate-200"
      )}
    >
      <div className="flex flex-wrap items-center gap-3">
        <StatusBadge status={backfill.status} />
        <span className="font-mono text-xs text-slate-900">
          {backfill.metrics.join(", ") || "—"}
        </span>
        <span className="text-sm text-slate-600">
          <LocalTime value={backfill.createdAt} />
        </span>
        <span className="ml-auto font-mono text-xs tabular-nums text-slate-400">
          {format.count(done)} / {format.count(total)}
        </span>
      </div>

      {running && (
        <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
          <div
            className="h-full rounded-full bg-brand-500 transition-[width] duration-500"
            style={{ width: `${Math.round(fraction * 100)}%` }}
          />
        </div>
      )}

      {failed && (
        <p className="mt-2 text-xs text-critical-700">
          This backfill did not finish, so the observations for{" "}
          <span className="font-mono">{backfill.metrics.join(", ")}</span> are
          incomplete for the payments it was catching up. Rules aggregating{" "}
          {backfill.metrics.length === 1 ? "it" : "them"} are reading across the
          hole and will not say so. Saving the profile again with the same
          metrics does not re-queue one — the metric is already declared, so
          nothing would be introduced.
        </p>
      )}

      {backfill.status === "Completed" && backfill.completedAt && (
        <p className="mt-1 text-xs text-slate-400">
          {t("organization.finished")} <LocalTime value={backfill.completedAt} />.
        </p>
      )}
    </div>
  );
}

const METRICS_AUDIT_FIELD = "Metrics";
const THRESHOLDS_AUDIT_FIELD = "Thresholds";

const CHANGE_STYLES: Record<string, string> = {
  added: "border-settled-200 bg-settled-50 text-settled-700",
  removed: "border-critical-200 bg-critical-50 text-critical-700",
  changed: "border-attention-200 bg-attention-50 text-attention-700",
  unchanged: "border-slate-200 bg-slate-50 text-slate-500",
};

function VelocityDeclarationChange({
  change,
  isInitialSnapshot,
}: {
  change: AuditLogChange;
  isInitialSnapshot: boolean;
}) {
  const isMetrics = change.field === METRICS_AUDIT_FIELD;

  const read = (value: unknown): { name: string; describe: string }[] | null => {
    if (isMetrics) {
      const parsed = parseVelocityMetrics(value);
      return parsed
        ? parsed.map((metric) => ({
            name: metric.name,
            describe: `← ${metric.path}`,
          }))
        : null;
    }
    const parsed = parseVelocityThresholds(value);
    return parsed
      ? parsed.map((threshold) => ({
          name: threshold.name,
          describe: `= ${threshold.value}`,
        }))
      : null;
  };

  const before = isInitialSnapshot ? null : read(change.oldValue);
  const after = read(change.newValue);

  if (after === null && before === null) {
    return (
      <p className="text-sm text-slate-400">
        Unreadable {isMetrics ? "metrics" : "thresholds"}
      </p>
    );
  }

  const rows = diffNamed(before, after).filter(
    (row) => isInitialSnapshot || row.change !== "unchanged"
  );

  if (rows.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        The declaration was rewritten without changing any{" "}
        {isMetrics ? "metric" : "threshold"}.
      </p>
    );
  }

  return (
    <ul className="space-y-2">
      {rows.map((row) => (
        <li key={row.name} className="space-y-1">
          <div className="flex flex-wrap items-baseline gap-2">
            <span
              className={cn(
                "rounded-md border px-1.5 py-0.5 font-mono text-[11px] uppercase tracking-wide",
                CHANGE_STYLES[row.change]
              )}
            >
              {isInitialSnapshot ? (isMetrics ? "metric" : "threshold") : row.change}
            </span>
            <span className="font-mono text-sm text-slate-900">{row.name}</span>
          </div>
          {row.before && (
            <p className="text-xs text-slate-500">
              <span className="text-slate-400">was</span>{" "}
              <span className="font-mono">{row.before}</span>
            </p>
          )}
          {row.after && (
            <p className="text-xs text-slate-500">
              <span className="text-slate-400">
                {row.before ? "now" : ""}
              </span>{" "}
              <span className="font-mono">{row.after}</span>
            </p>
          )}
        </li>
      ))}
    </ul>
  );
}

function VelocityProfileHistory({
  organizationSegment,
  onUnauthorized,
}: {
  organizationSegment: string;
  onUnauthorized: () => void;
}) {
  return (
    <HistoryPanel
      entityType="VelocityProfile"
      path={historyPath.VelocityProfile(organizationSegment)}
      onUnauthorized={onUnauthorized}
      renderChange={(change, { isInitialSnapshot }) =>
        change.field === METRICS_AUDIT_FIELD ||
        change.field === THRESHOLDS_AUDIT_FIELD ? (
          <VelocityDeclarationChange
            change={change}
            isInitialSnapshot={isInitialSnapshot}
          />
        ) : null
      }
      lifecycleNotes={{
        softDeleted:
          "Recording stopped — no observation was written from here until the profile came back. Everything taken before it was kept, and any window spanning the gap is short of the payments in it.",
        recovered:
          "Recording resumed with the declaration exactly as it stood. The gap is not filled by this; re-declaring a metric is what backfills it.",
      }}
      scopeNote="Only the declaration is recorded here. The observations it caused are not — there are far too many of them, and none of them is anybody's decision."
    />
  );
}
