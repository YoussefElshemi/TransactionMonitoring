"use client";

import { RotateCcw } from "lucide-react";

import { cn } from "@/lib/utils";
import {
  CASE_GROUPING_SCOPES,
  CASE_GROUPING_SEVERITY_POLICIES,
  DEFAULT_CASE_GROUPING_ENABLED,
  DEFAULT_CASE_GROUPING_PERIOD_MINUTES,
  DEFAULT_CASE_GROUPING_SCOPE,
  DEFAULT_CASE_GROUPING_SEVERITY_POLICY,
  DEFAULT_DEDUPLICATION_ENABLED,
  DEFAULT_DEDUPLICATION_WINDOW_MINUTES,
  caseGroupingScopeLabel,
  caseGroupingScopeText,
  caseGroupingSeverityPolicyLabel,
  caseGroupingSeverityPolicyText,
  describeMinutes,
} from "@/lib/alertPolicy";
import { INHERIT } from "@/components/HoldPolicyFields";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export interface AlertPolicy {
  caseGroupingEnabled: boolean | null;
  deduplicationEnabled: boolean | null;
  caseGroupingPeriod: number | null;
  deduplicationWindow: number | null;
  caseGroupingScope: string | null;
  caseGroupingSeverityPolicy: string | null;
  maxAlertsPerCase: number | null;
}

export const EMPTY_ALERT_POLICY: AlertPolicy = {
  caseGroupingEnabled: null,
  deduplicationEnabled: null,
  caseGroupingPeriod: null,
  deduplicationWindow: null,
  caseGroupingScope: null,
  caseGroupingSeverityPolicy: null,
  maxAlertsPerCase: null,
};

export const isFullyInherited = (policy: AlertPolicy): boolean =>
  Object.values(policy).every((value) => value === null);

function MinutesField({
  label,
  value,
  fallback,
  disabled,
  error,
  onChange,
}: {
  label: string;
  value: number | null;
  fallback: number;
  disabled?: boolean;
  error?: string;
  onChange: (next: number | null) => void;
}) {
  const t = useTranslation();
  return (
    <div className="w-56 space-y-1.5">
      <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label} (min)
      </label>
      <Input
        type="number"
        min={1}
        placeholder={t("alertPolicyFields.default")}
        value={value ?? ""}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
        className={
          error ? "border-critical-500 focus-visible:ring-critical-500" : undefined
        }
        onChange={(event) => {
          const raw = event.target.value;
          if (raw === "") return onChange(null);
          onChange(Math.max(1, Number(raw) || 1));
        }}
      />
      <p className="text-xs text-slate-500">
        {value === null
          ? `Default: ${fallback} minutes — ${describeMinutes(fallback)}`
          : `${value} minutes — ${describeMinutes(value)}`}
      </p>
      {error && <p className="text-xs text-critical-700">{error}</p>}
    </div>
  );
}

export function AlertPolicyFields({
  value,
  onChange,
  disabled = false,
  errors,
}: {
  value: AlertPolicy;
  onChange: (next: AlertPolicy) => void;
  disabled?: boolean;
  errors?: Partial<Record<keyof AlertPolicy, string>>;
}) {
  const t = useTranslation();
  const grouping = value.caseGroupingEnabled ?? DEFAULT_CASE_GROUPING_ENABLED;
  const deduplication =
    value.deduplicationEnabled ?? DEFAULT_DEDUPLICATION_ENABLED;

  const scope = value.caseGroupingScope ?? DEFAULT_CASE_GROUPING_SCOPE;
  const severityPolicy =
    value.caseGroupingSeverityPolicy ?? DEFAULT_CASE_GROUPING_SEVERITY_POLICY;

  const set = <K extends keyof AlertPolicy>(key: K, next: AlertPolicy[K]) =>
    onChange({ ...value, [key]: next });

  const groupingOff = disabled || !grouping;

  return (
    <div className="space-y-6">
      <section className="space-y-3">
        <PolicySwitch
          label={t("alertPolicyFields.suppressDuplicateAlerts")}
          checked={deduplication}
          inherited={value.deduplicationEnabled === null}
          disabled={disabled}
          onChange={(next) => set("deduplicationEnabled", next)}
        >
          {t("alertPolicyFields.asks")} <em>is this the same finding?</em> — the same party breaching the
          same rule again inside the window. The repeat is suppressed and closed
          automatically against the alert it duplicated, so it stays on the
          record without being handed to anyone twice. On unless you turn it
          off.
        </PolicySwitch>

        <div className="pl-12">
          {deduplication ? (
            <MinutesField
              label={t("alertPolicyFields.deduplicationWindow")}
              value={value.deduplicationWindow}
              fallback={DEFAULT_DEDUPLICATION_WINDOW_MINUTES}
              disabled={disabled}
              error={errors?.deduplicationWindow}
              onChange={(next) => set("deduplicationWindow", next)}
            />
          ) : (
            <p className="max-w-prose text-xs text-slate-500">
              Every repeat becomes its own alert, however many times the same
              rule fires on the same party.
            </p>
          )}
        </div>
      </section>

      <section className="space-y-3 border-t border-slate-100 pt-6">
        <PolicySwitch
          label={t("alertPolicyFields.groupAlertsIntoCasesBy")}
          checked={grouping}
          inherited={value.caseGroupingEnabled === null}
          disabled={disabled}
          onChange={(next) => set("caseGroupingEnabled", next)}
        >
          {t("alertPolicyFields.asks")} <em>is there already work open on this subject?</em> — a new
          transaction joins the party&apos;s open case instead of opening its
          own. Off unless you opt in, because it decides how many pieces of work
          the desk is handed: separate transactions arrive folded into one case
          rather than as one case each.
        </PolicySwitch>

        <div
          className={cn(
            "space-y-4 pl-12 transition-opacity",
            !grouping && "opacity-60"
          )}
        >
          {!grouping && (
            <p className="max-w-prose text-xs text-slate-500">
              Grouping is off, so every alert opens its own case. Everything
              below applies only once it is on.
            </p>
          )}

          <div className="flex flex-wrap items-start gap-4">
            <MinutesField
              label={t("alertPolicyFields.caseGroupingPeriod")}
              value={value.caseGroupingPeriod}
              fallback={DEFAULT_CASE_GROUPING_PERIOD_MINUTES}
              disabled={groupingOff}
              error={errors?.caseGroupingPeriod}
              onChange={(next) => set("caseGroupingPeriod", next)}
            />

            <div className="w-56 space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("alertPolicyFields.maxAlertsPerCase")}
              </label>
              <Input
                type="number"
                min={1}
                placeholder={t("alertPolicyFields.uncapped")}
                value={value.maxAlertsPerCase ?? ""}
                disabled={groupingOff}
                aria-invalid={errors?.maxAlertsPerCase ? true : undefined}
                className={
                  errors?.maxAlertsPerCase
                    ? "border-critical-500 focus-visible:ring-critical-500"
                    : undefined
                }
                onChange={(event) => {
                  const raw = event.target.value;
                  if (raw === "") return set("maxAlertsPerCase", null);
                  set("maxAlertsPerCase", Math.max(1, Number(raw) || 1));
                }}
              />
              <p className="text-xs text-slate-500">
                {value.maxAlertsPerCase === null
                  ? "Default: uncapped — a case holds however many alerts the period brings it."
                  : `Once a case holds ${value.maxAlertsPerCase}, the next transaction opens a fresh one.`}
              </p>
              {errors?.maxAlertsPerCase && (
                <p className="text-xs text-critical-700">
                  {errors.maxAlertsPerCase}
                </p>
              )}
            </div>
          </div>

          <p className="max-w-prose text-xs text-slate-500">
            A cap is the same problem grouping solves, approached from the other
            side: a case is a unit of work somebody has to read, and one party
            having a bad afternoon should not pour a hundred transactions into a
            single unreviewable case. Suppressed alerts count towards it, since
            they are filed against the case and show on it.
          </p>

          <div className="flex flex-wrap items-start gap-4">
            <div className="min-w-64 max-w-sm flex-1 space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("alertPolicyFields.groupingScope")}
              </label>
              <Select
                value={value.caseGroupingScope ?? INHERIT}
                onValueChange={(next) =>
                  set("caseGroupingScope", next === INHERIT ? null : next)
                }
                disabled={groupingOff}
              >
                <SelectTrigger
                  aria-invalid={errors?.caseGroupingScope ? true : undefined}
                  className={
                    errors?.caseGroupingScope
                      ? "border-critical-500 focus:ring-critical-500"
                      : undefined
                  }
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={INHERIT}>
                    Default —{" "}
                    {caseGroupingScopeLabel(
                      DEFAULT_CASE_GROUPING_SCOPE
                    ).toLowerCase()}
                  </SelectItem>
                  {CASE_GROUPING_SCOPES.map((option) => (
                    <SelectItem key={option} value={option}>
                      {caseGroupingScopeLabel(option)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                {caseGroupingScopeText(scope)}
              </p>
              {errors?.caseGroupingScope && (
                <p className="text-xs text-critical-700">
                  {errors.caseGroupingScope}
                </p>
              )}
            </div>

            <div className="min-w-64 max-w-sm flex-1 space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("alertPolicyFields.severityPolicy")}
              </label>
              <Select
                value={value.caseGroupingSeverityPolicy ?? INHERIT}
                onValueChange={(next) =>
                  set(
                    "caseGroupingSeverityPolicy",
                    next === INHERIT ? null : next
                  )
                }
                disabled={groupingOff}
              >
                <SelectTrigger
                  aria-invalid={
                    errors?.caseGroupingSeverityPolicy ? true : undefined
                  }
                  className={
                    errors?.caseGroupingSeverityPolicy
                      ? "border-critical-500 focus:ring-critical-500"
                      : undefined
                  }
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={INHERIT}>
                    Default —{" "}
                    {caseGroupingSeverityPolicyLabel(
                      DEFAULT_CASE_GROUPING_SEVERITY_POLICY
                    ).toLowerCase()}
                  </SelectItem>
                  {CASE_GROUPING_SEVERITY_POLICIES.map((option) => (
                    <SelectItem key={option} value={option}>
                      {caseGroupingSeverityPolicyLabel(option)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                {caseGroupingSeverityPolicyText(severityPolicy)}
              </p>
              {errors?.caseGroupingSeverityPolicy && (
                <p className="text-xs text-critical-700">
                  {errors.caseGroupingSeverityPolicy}
                </p>
              )}
            </div>
          </div>
        </div>
      </section>

      {!isFullyInherited(value) && !disabled && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => onChange(EMPTY_ALERT_POLICY)}
          title={t("alertPolicyFields.clearEveryFieldSoThis")}
        >
          <RotateCcw className="size-3.5" />
          {t("alertPolicyFields.resetToDefaults")}
        </Button>
      )}
    </div>
  );
}

export const alertPolicyPatch = (
  next: AlertPolicy,
  current: AlertPolicy
): Record<string, number | string | boolean | null> => {
  const patch: Record<string, number | string | boolean | null> = {};
  for (const key of Object.keys(EMPTY_ALERT_POLICY) as (keyof AlertPolicy)[]) {
    if (next[key] !== current[key]) patch[key] = next[key];
  }
  return patch;
};
