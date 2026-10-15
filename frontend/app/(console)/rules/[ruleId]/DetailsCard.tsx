"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";

import { Rule, RuleAction, RuleResource, RuleSeverity } from "@/types";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { MatchPolarityBadge } from "@/components/MatchPolarityBadge";
import {
  HoldPolicy,
  HoldPolicyFields,
  holdPolicyPatch,
} from "@/components/HoldPolicyFields";
import {
  RuleAlertPolicy,
  RuleAlertPolicyFields,
  ruleAlertPolicyPatch,
} from "@/components/RuleAlertPolicyFields";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { API_BASE, reportApiError } from "./api";
import { unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const FRIENDLY_NAME_MAX = 50;
const ACTION_OPTIONS: RuleAction[] = ["Allow", "Flag", "Review", "Block"];
const SEVERITY_OPTIONS: RuleSeverity[] = [
  "Info",
  "Low",
  "Medium",
  "High",
  "Critical",
];

const FIELD_LABELS: Record<string, string> = {
  "Body.FriendlyName": "Friendly name",
  "Body.IsEnabled": "Enabled",
  "Body.Action": "Action",
  "Body.Severity": "Severity",
  "Body.RiskWeight": "Risk weight",
  "Body.HoldWindow": "Hold window",
  "Body.HoldExpiryVerdict": "When the hold expires",
  "Body.AlertSubjectRole": "Alert subject",
  "Body.DeduplicationWindow": "Deduplication window",
};

const FIELD_OF: Record<string, keyof RuleAlertPolicy> = {
  "Body.AlertSubjectRole": "alertSubjectRole",
  "Body.DeduplicationWindow": "deduplicationWindow",
};

interface DetailsCardProps {
  rule: Rule;
  canEdit: boolean;
  onSaved: (rule: Rule) => void;
  onUnauthorized: () => void;
}

export function DetailsCard({
  rule,
  canEdit,
  onSaved,
  onUnauthorized,
}: DetailsCardProps) {
  const t = useTranslation();
  const [friendlyName, setFriendlyName] = useState<string>(rule.friendlyName);
  const [isEnabled, setIsEnabled] = useState<boolean>(rule.isEnabled);
  const [action, setAction] = useState<RuleAction>(rule.action);
  const [severity, setSeverity] = useState<RuleSeverity>(rule.severity);
  const [riskWeight, setRiskWeight] = useState<number>(rule.riskWeight);
  const [holdPolicy, setHoldPolicy] = useState<HoldPolicy>({
    holdWindow: rule.holdWindow,
    holdExpiryVerdict: rule.holdExpiryVerdict,
  });
  // `?? null` rather than a bare read: the API returns neither field on a rule,
  // so both arrive `undefined`. Left as-is they are not `null`, which is what
  // the fields below test for to mean "inherit" — so an unset window read as a
  // set one and rendered as `NaN hours NaN minutes`. Normalised once here so
  // nothing downstream has to know the API omits them.
  const [alertPolicy, setAlertPolicy] = useState<RuleAlertPolicy>({
    alertSubjectRole: rule.alertSubjectRole ?? null,
    deduplicationWindow: rule.deduplicationWindow ?? null,
  });
  const [saving, setSaving] = useState<boolean>(false);
  const [alertPolicyErrors, setAlertPolicyErrors] = useState<
    Partial<Record<keyof RuleAlertPolicy, string>>
  >({});

  const { user } = useAuth();

  const savedHoldPolicy: HoldPolicy = {
    holdWindow: rule.holdWindow,
    holdExpiryVerdict: rule.holdExpiryVerdict,
  };

  const savedAlertPolicy: RuleAlertPolicy = {
    alertSubjectRole: rule.alertSubjectRole ?? null,
    deduplicationWindow: rule.deduplicationWindow ?? null,
  };

  useEffect(() => {
    setFriendlyName(rule.friendlyName);
    setIsEnabled(rule.isEnabled);
    setAction(rule.action);
    setSeverity(rule.severity);
    setRiskWeight(rule.riskWeight);
    setHoldPolicy({
      holdWindow: rule.holdWindow,
      holdExpiryVerdict: rule.holdExpiryVerdict,
    });
    setAlertPolicy({
      alertSubjectRole: rule.alertSubjectRole ?? null,
      deduplicationWindow: rule.deduplicationWindow ?? null,
    });
    setAlertPolicyErrors({});
  }, [
    rule.friendlyName,
    rule.isEnabled,
    rule.action,
    rule.severity,
    rule.riskWeight,
    rule.holdWindow,
    rule.holdExpiryVerdict,
    rule.alertSubjectRole,
    rule.deduplicationWindow,
  ]);

  const nameChanged = friendlyName !== rule.friendlyName;
  const enabledChanged = isEnabled !== rule.isEnabled;
  const actionChanged = action !== rule.action;
  const severityChanged = severity !== rule.severity;
  const riskWeightChanged = riskWeight !== rule.riskWeight;
  const holdPolicyChanged =
    Object.keys(holdPolicyPatch(holdPolicy, savedHoldPolicy)).length > 0;
  const alertPolicyChanged =
    Object.keys(ruleAlertPolicyPatch(alertPolicy, savedAlertPolicy)).length > 0;
  const dirty =
    nameChanged ||
    enabledChanged ||
    actionChanged ||
    severityChanged ||
    riskWeightChanged ||
    holdPolicyChanged ||
    alertPolicyChanged;

  const reset = () => {
    setFriendlyName(rule.friendlyName);
    setIsEnabled(rule.isEnabled);
    setAction(rule.action);
    setSeverity(rule.severity);
    setRiskWeight(rule.riskWeight);
    setHoldPolicy(savedHoldPolicy);
    setAlertPolicy(savedAlertPolicy);
    setAlertPolicyErrors({});
  };

  const save = async () => {
    if (!dirty) return;
    if (nameChanged && !friendlyName.trim()) {
      return toast.error(t("rules.friendlyNameRequired"));
    }

    const payload: {
      friendlyName?: string;
      isEnabled?: boolean;
      action?: RuleAction;
      severity?: RuleSeverity;
      riskWeight?: number;
    } & Record<string, unknown> = {};
    if (nameChanged) payload.friendlyName = friendlyName.trim();
    if (enabledChanged) payload.isEnabled = isEnabled;
    if (actionChanged) payload.action = action;
    if (severityChanged) payload.severity = severity;
    if (riskWeightChanged) payload.riskWeight = riskWeight;
    Object.assign(payload, holdPolicyPatch(holdPolicy, savedHoldPolicy));
    Object.assign(payload, ruleAlertPolicyPatch(alertPolicy, savedAlertPolicy));

    setSaving(true);
    setAlertPolicyErrors({});
    try {
      const response = await apiFetch(`${API_BASE}/rules/${rule.ruleId}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        credentials: "include",
      });

      if (response.ok) {
        const data = await unwrap<RuleResource>(response);
        toast.success(t("rules.detailsUpdated"));
        onSaved(data.rule);
      } else {
        if (response.status === 400) {
          const body = await response
            .clone()
            .json()
            .catch(() => null);
          const errors = (body as { errors?: Record<string, string[]> } | null)
            ?.errors;
          if (errors) {
            const next: Partial<Record<keyof RuleAlertPolicy, string>> = {};
            for (const [key, messages] of Object.entries(errors)) {
              const field = FIELD_OF[key];
              if (field) next[field] = messages.join(" ");
            }
            setAlertPolicyErrors(next);
          }
        }

        await reportApiError(response, {
          fallback: t("rules.failedUpdateDetails"),
          onUnauthorized,
          fieldLabels: FIELD_LABELS,
        });
      }
    } catch {
      toast.error(t("rules.failedUpdateDetails"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="rounded-lg border border-slate-200 bg-card p-4">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-slate-900">{t("common.details")}</h2>
        <div className="flex flex-wrap items-center gap-1.5">
          <StatusBadge status={rule.isEnabled ? "Enabled" : "Disabled"} />
          <StatusBadge status={rule.action} />
          <SeverityBadge severity={rule.severity} />
          <MatchPolarityBadge matchPolarity={rule.matchPolarity} />
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-4">
        <div className="min-w-64 flex-1 space-y-1.5">
          <label
            htmlFor="friendlyName"
            className="font-mono text-xs uppercase tracking-widest text-slate-400"
          >
            {t("rules.friendlyName")}
          </label>
          <Input
            id="friendlyName"
            value={friendlyName}
            maxLength={FRIENDLY_NAME_MAX}
            disabled={!canEdit || saving}
            onChange={(e) => setFriendlyName(e.target.value)}
          />
        </div>

        <label className="inline-flex h-10 items-center gap-2 font-mono text-sm text-slate-700">
          <input
            type="checkbox"
            checked={isEnabled}
            disabled={!canEdit || saving}
            onChange={(e) => setIsEnabled(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 accent-brand-800"
          />
          {t("common.enabled")}
        </label>

        {canEdit && (
          <div className="ml-auto flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={reset}
              disabled={!dirty || saving}
            >
              {t("common.reset")}
            </Button>
            <Button size="sm" onClick={save} disabled={!dirty || saving}>
              {saving ? "Saving..." : "Save details"}
            </Button>
          </div>
        )}
      </div>

      <div className="mt-4 flex flex-wrap items-end gap-4 border-t border-slate-100 pt-4">
        <div className="w-40 space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("rules.action")}
          </label>
          <Select
            value={action}
            onValueChange={(v) => setAction(v as RuleAction)}
            disabled={!canEdit || saving}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {ACTION_OPTIONS.map((a) => (
                <SelectItem key={a} value={a}>
                  {a}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="w-40 space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("common.severity")}
          </label>
          <Select
            value={severity}
            onValueChange={(v) => setSeverity(v as RuleSeverity)}
            disabled={!canEdit || saving}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {SEVERITY_OPTIONS.map((s) => (
                <SelectItem key={s} value={s}>
                  {s}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="w-32 space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("rules.riskWeight")}
          </label>
          <Input
            type="number"
            min={0}
            max={100}
            value={riskWeight}
            disabled={!canEdit || saving}
            onChange={(e) =>
              setRiskWeight(Math.max(0, Math.min(100, Number(e.target.value) || 0)))
            }
          />
        </div>

        <div className="space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("rules.ruleFiresWhen")}
          </label>
          <div className="flex h-10 items-center">
            <MatchPolarityBadge matchPolarity={rule.matchPolarity} />
          </div>
        </div>
      </div>

      <div className="mt-4 border-t border-slate-100 pt-4">
        <div className="mb-3">
          <h3 className="text-sm font-semibold text-slate-900">{t("rules.holdPolicy")}</h3>
          <p className="mt-0.5 text-xs text-slate-500">
            What happens to a transaction this rule holds if nobody reviews it
            in time. Only applies when the rule&apos;s action is Review — an
            Allow, Flag or Block verdict never holds anything.
          </p>
        </div>
        <HoldPolicyFields
          value={holdPolicy}
          onChange={setHoldPolicy}
          disabled={!canEdit || saving}
          inheritance={{ organization: user?.organization ?? null }}
        />
      </div>

      <div className="mt-4 border-t border-slate-100 pt-4">
        <div className="mb-3">
          <h3 className="text-sm font-semibold text-slate-900">{t("rules.alertPolicy")}</h3>
          <p className="mt-0.5 text-xs text-slate-500">
            Who this rule&apos;s findings are filed against, and how quickly a
            repeat of one folds into the alert already open.
          </p>
        </div>
        <RuleAlertPolicyFields
          value={alertPolicy}
          onChange={(next) => {
            setAlertPolicy(next);
            setAlertPolicyErrors({});
          }}
          disabled={!canEdit || saving}
          inheritance={{ organization: user?.organization ?? null }}
          errors={alertPolicyErrors}
        />
      </div>

      <p className="mt-3 text-xs text-slate-400">
        Name, enabled state, action, severity, risk weight, hold policy and
        alert policy apply immediately. Changing the rule logic goes through
        versions and
        deployments. Risk weight only matters for escalation — it&apos;s
        added to the risk score when this rule fires, and 0 means it never
        contributes. A hold already running keeps the deadline it started
        with, so changing this never moves a date someone has been shown.
        Alerts already raised keep the case they were filed into — the alert
        policy only decides where later findings land.
      </p>
    </div>
  );
}
