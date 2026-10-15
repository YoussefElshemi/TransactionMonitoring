"use client";

import { RotateCcw } from "lucide-react";

import { Organization, PARTY_ROLES } from "@/types";
import {
  DEFAULT_ALERT_SUBJECT_ROLE,
  DEFAULT_DEDUPLICATION_WINDOW_MINUTES,
  describeMinutes
} from "@/lib/alertPolicy";
import { INHERIT } from "@/components/HoldPolicyFields";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export interface RuleAlertPolicy {
  alertSubjectRole: string | null;
  deduplicationWindow: number | null;
}

export const EMPTY_RULE_ALERT_POLICY: RuleAlertPolicy = {
  alertSubjectRole: null,
  deduplicationWindow: null
};

export interface RuleAlertPolicyInheritance {
  organization: Pick<Organization, "deduplicationWindow"> | null;
}

const effectiveWindow = (
  policy: RuleAlertPolicy,
  { organization }: RuleAlertPolicyInheritance
): { minutes: number; from: string } => {
  if (policy.deduplicationWindow != null)
    return { minutes: policy.deduplicationWindow, from: "this rule" };
  if (organization?.deduplicationWindow != null)
    return {
      minutes: organization.deduplicationWindow,
      from: "the organisation"
    };
  return {
    minutes: DEFAULT_DEDUPLICATION_WINDOW_MINUTES,
    from: "the server default"
  };
};

const effectiveRole = (
  policy: RuleAlertPolicy
): { role: string; from: string } =>
  policy.alertSubjectRole != null
    ? { role: policy.alertSubjectRole, from: "this rule" }
    : { role: DEFAULT_ALERT_SUBJECT_ROLE, from: "the server default" };

export function RuleAlertPolicyFields({
  value,
  onChange,
  inheritance,
  disabled = false,
  errors
}: {
  value: RuleAlertPolicy;
  onChange: (next: RuleAlertPolicy) => void;
  inheritance: RuleAlertPolicyInheritance;
  disabled?: boolean;
  errors?: Partial<Record<keyof RuleAlertPolicy, string>>;
}) {
  const t = useTranslation();
  const inherited =
    value.alertSubjectRole == null && value.deduplicationWindow == null;

  const role = effectiveRole(value);
  const window = effectiveWindow(value, inheritance);

  const roleOptions: string[] =
    value.alertSubjectRole != null &&
    !(PARTY_ROLES as string[]).includes(value.alertSubjectRole)
      ? [...PARTY_ROLES, value.alertSubjectRole]
      : PARTY_ROLES;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-end gap-4">
        <div className="min-w-56 max-w-sm flex-1 space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("ruleAlertPolicyFields.alertSubject")}
          </label>
          <Select
            value={value.alertSubjectRole ?? INHERIT}
            onValueChange={(next) =>
              onChange({
                ...value,
                alertSubjectRole: next === INHERIT ? null : next
              })
            }
            disabled={disabled}
          >
            <SelectTrigger
              aria-invalid={errors?.alertSubjectRole ? true : undefined}
              className={
                errors?.alertSubjectRole
                  ? "border-critical-500 focus:ring-critical-500"
                  : undefined
              }
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={INHERIT}>
                Inherit the default ({DEFAULT_ALERT_SUBJECT_ROLE})
              </SelectItem>
              {roleOptions.map((option) => (
                <SelectItem key={option} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="w-56 shrink-0 space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("ruleAlertPolicyFields.deduplicationWindowMin")}
          </label>
          <Input
            type="number"
            min={1}
            placeholder={t("ruleAlertPolicyFields.inherit")}
            value={value.deduplicationWindow ?? ""}
            disabled={disabled}
            aria-invalid={errors?.deduplicationWindow ? true : undefined}
            className={
              errors?.deduplicationWindow
                ? "border-critical-500 focus-visible:ring-critical-500"
                : undefined
            }
            onChange={(event) => {
              const raw = event.target.value;
              if (raw === "") {
                return onChange({ ...value, deduplicationWindow: null });
              }
              onChange({
                ...value,
                deduplicationWindow: Math.max(1, Number(raw) || 1)
              });
            }}
          />
        </div>

        {!inherited && !disabled && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() => onChange(EMPTY_RULE_ALERT_POLICY)}
            title={t("ruleAlertPolicyFields.clearBothFieldsSoThis")}
          >
            <RotateCcw className="size-3.5" />
            {t("ruleAlertPolicyFields.resetToInherit")}
          </Button>
        )}
      </div>

      {(errors?.alertSubjectRole || errors?.deduplicationWindow) && (
        <div className="space-y-0.5">
          {errors.alertSubjectRole && (
            <p className="text-xs text-critical-700">
              Alert subject: {errors.alertSubjectRole}
            </p>
          )}
          {errors.deduplicationWindow && (
            <p className="text-xs text-critical-700">
              Deduplication window: {errors.deduplicationWindow}
            </p>
          )}
        </div>
      )}

      <div className="max-w-prose space-y-1.5 text-xs text-slate-500">
        <p>
          Which party the alert is about. A sanctions rule watches the
          beneficiary; a structuring rule watches the remitter. There is no
          organisation-wide setting for this one — left alone it is{" "}
          {DEFAULT_ALERT_SUBJECT_ROLE}.
        </p>
        <p>
          A second finding by this rule on the same party inside the
          deduplication window is suppressed into the alert already open rather
          than raising another — the transaction is still held or blocked either
          way. Left empty the window follows the organisation&apos;s setting,
          then {DEFAULT_DEDUPLICATION_WINDOW_MINUTES} minutes.
        </p>
        <p>
          Both only bite where your organisation resolves parties — with no
          party mapping there is nobody to file against and nothing to
          deduplicate.
        </p>
      </div>

      {/* `== null` rather than `=== null`, deliberately: this component is fed
          from a rule read, and the API sends neither field — so an unset one
          arrives `undefined`. A strict check reads that as "explicitly set",
          takes the branch below, and formats a missing number. The loose check
          catches both spellings of absent. */}
      {value.alertSubjectRole == null || value.deduplicationWindow == null ? (
        <p className="text-xs text-slate-500">
          In effect:{" "}
          <span className="font-medium text-slate-700">
            alerts about the {role.role.toLowerCase()}, deduplicated over{" "}
            {describeMinutes(window.minutes)}
          </span>{" "}
          — subject from {role.from}, window from {window.from}.
        </p>
      ) : (
        <p className="text-xs text-slate-500">
          Deduplication window: {describeMinutes(value.deduplicationWindow)}.
        </p>
      )}
    </div>
  );
}

export const ruleAlertPolicyPatch = (
  next: RuleAlertPolicy,
  current: RuleAlertPolicy
): Record<string, number | string | null> => {
  const patch: Record<string, number | string | null> = {};
  if (next.alertSubjectRole !== current.alertSubjectRole) {
    patch.alertSubjectRole = next.alertSubjectRole;
  }
  if (next.deduplicationWindow !== current.deduplicationWindow) {
    patch.deduplicationWindow = next.deduplicationWindow;
  }
  return patch;
};
