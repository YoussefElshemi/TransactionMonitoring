"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Save, TriangleAlert } from "lucide-react";

import {
  SuspiciousActivityReportPolicy,
  SuspiciousActivityReportPolicyBody,
  ValidationProblemDetails,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { hasWildcard } from "@/lib/payloadPath";
import { FILINGS_HREF } from "@/lib/suspiciousActivityReports";
import { useTransactionSchema } from "@/lib/useTransactionSchema";
import { cn } from "@/lib/utils";
import { PolicySwitch } from "@/components/PolicySwitch";
import { SchemaFieldCombobox } from "@/components/SchemaFieldCombobox";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const fieldError = (
  errors: Record<string, string[]> | undefined,
  field: string
): string | null => {
  if (!errors) return null;
  const wanted = field.toLowerCase();
  const key = Object.keys(errors).find((candidate) => {
    const lowered = candidate.toLowerCase();
    return lowered === wanted || lowered.endsWith(`.${wanted}`);
  });
  return key ? errors[key].join("\n") : null;
};

interface PolicyDraft {
  reportingEntityId: string;
  filingDeadlineDays: string;
  requireFilingApproval: boolean;
  amountPath: string;
  currencyPath: string;
}

const EMPTY_DRAFT: PolicyDraft = {
  reportingEntityId: "",
  filingDeadlineDays: "",
  requireFilingApproval: false,
  amountPath: "",
  currencyPath: "",
};

const draftFrom = (policy: SuspiciousActivityReportPolicy): PolicyDraft => ({
  reportingEntityId: policy.reportingEntityId ?? "",
  filingDeadlineDays:
    policy.filingDeadlineDays === null ? "" : String(policy.filingDeadlineDays),
  requireFilingApproval: policy.requireFilingApproval,
  amountPath: policy.amountPath ?? "",
  currencyPath: policy.currencyPath ?? "",
});

const policyBody = (
  draft: PolicyDraft
): SuspiciousActivityReportPolicyBody => {
  const days = draft.filingDeadlineDays.trim();
  return {
    reportingEntityId: draft.reportingEntityId.trim() || null,
    filingDeadlineDays: days === "" ? null : Number(days),
    requireFilingApproval: draft.requireFilingApproval,
    amountPath: draft.amountPath.trim() || null,
    currencyPath: draft.currencyPath.trim() || null,
  };
};

export function SuspiciousActivityReportPolicyPanel({
  organizationId,
  onUnauthorized,
  suggestSchemaFields = true,
}: {
  organizationId: string;
  onUnauthorized: () => void;
  suggestSchemaFields?: boolean;
}) {
  const t = useTranslation();
  const [draft, setDraft] = useState<PolicyDraft>(EMPTY_DRAFT);
  const [effectiveDays, setEffectiveDays] = useState<number | null>(null);
  const [stated, setStated] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string | null>>({});

  const { fields: schemaFields } = useTransactionSchema(suggestSchemaFields);

  const fetchPolicy = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/suspicious-activity-report-policy`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const policy = await unwrap<SuspiciousActivityReportPolicy>(response);
        setDraft(draftFrom(policy));
        setEffectiveDays(policy.effectiveFilingDeadlineDays);
        setStated(true);
        return;
      }

      if (response.status === 404) {
        setDraft(EMPTY_DRAFT);
        setEffectiveDays(null);
        setStated(false);
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.errorOccurredFetchingFilingPolicy"),
        onUnauthorized,
        toastId: "error-fetching-filing-policy",
      });
    } catch {
      toast.error(t("organization.errorOccurredFetchingFilingPolicy"), {
        id: "error-fetching-filing-policy",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  useEffect(() => {
    fetchPolicy();
  }, [fetchPolicy]);

  const update = (patch: Partial<PolicyDraft>) =>
    setDraft((current) => ({ ...current, ...patch }));

  const clearError = (field: string) =>
    setErrors((current) => ({ ...current, [field]: null }));

  const handleSave = async () => {
    setSaving(true);
    setErrors({});

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/suspicious-activity-report-policy`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(policyBody(draft)),
        }
      );

      if (response.ok) {
        toast.success(t("organization.filingPolicySaved"), {
          description:
            t("organization.appliesFilingsOpenedFromNow"),
        });
        await fetchPolicy();
        return;
      }

      if (response.status === 400) {
        const problem = (await response.json().catch(() => null)) as
          | ValidationProblemDetails
          | null;
        const next: Record<string, string | null> = {
          reportingEntityId: fieldError(problem?.errors, "reportingEntityId"),
          filingDeadlineDays: fieldError(problem?.errors, "filingDeadlineDays"),
          amountPath: fieldError(problem?.errors, "amountPath"),
          currencyPath: fieldError(problem?.errors, "currencyPath"),
        };
        setErrors(next);

        if (!Object.values(next).some(Boolean)) {
          toast.error(problem?.detail || t("organization.filingPolicyRefused"));
        }
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedSaveFilingPolicy"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedSaveFilingPolicy"));
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

  const typedDays = draft.filingDeadlineDays.trim();
  const deadlineInherited = typedDays === "";

  return (
    <div className="space-y-4">
      {stated === false && (
        <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
          Your organisation has not stated a filing policy. Filings still work —
          they run on the house defaults below. Fill anything in here and save to
          state your own.
        </p>
      )}

      <Card>
        <CardContent className="space-y-6 pt-6">
          <section className="max-w-md space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("organization.reportingEntityId")}
            </label>
            <Input
              value={draft.reportingEntityId}
              onChange={(event) => {
                update({ reportingEntityId: event.target.value });
                clearError("reportingEntityId");
              }}
              disabled={saving}
              placeholder="RE-01234"
              className={cn(
                "font-mono",
                errors.reportingEntityId &&
                  "border-critical-500 focus-visible:ring-critical-500"
              )}
            />
            <p className="max-w-prose text-xs text-slate-500">
              The identifier your financial intelligence unit issued to your
              organisation — goAML calls it <code>rentity_id</code>. Until it is
              recorded here, <strong>exporting a filing refuses</strong>: a goAML
              file without one is not something a unit will take.
            </p>
            {errors.reportingEntityId && (
              <p className="whitespace-pre-line text-xs text-critical-700">
                {errors.reportingEntityId}
              </p>
            )}
          </section>

          <section className="space-y-1.5 border-t border-slate-100 pt-6">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("organization.filingDeadline")}
            </label>
            <div className="flex flex-wrap items-center gap-3">
              <Input
                value={draft.filingDeadlineDays}
                onChange={(event) => {
                  update({ filingDeadlineDays: event.target.value });
                  clearError("filingDeadlineDays");
                }}
                disabled={saving}
                inputMode="numeric"
                placeholder={t("organization.leaveBlankForTheDefault")}
                className={cn(
                  "w-56 font-mono",
                  errors.filingDeadlineDays &&
                    "border-critical-500 focus-visible:ring-critical-500"
                )}
              />
              <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                days
              </span>

              {effectiveDays !== null ? (
                <span className="text-sm text-slate-600">
                  A filing opened now gets{" "}
                  <span className="font-mono font-medium text-slate-900">
                    {effectiveDays} {effectiveDays === 1 ? "day" : "days"}
                  </span>
                  {deadlineInherited && " — the house default"}.
                </span>
              ) : (
                <span className="text-sm text-slate-500">
                  {t("organization.saveThePolicyToSee")}
                </span>
              )}
            </div>
            <p className="max-w-prose text-xs text-slate-500">
              Leave it blank to use the house default. Whatever this says, a
              filing&apos;s own deadline is <strong>fixed at the moment its case
              opened</strong> and never recalculated — changing this number
              changes what future filings are held to and moves nothing that is
              already open.
            </p>
            {errors.filingDeadlineDays && (
              <p className="whitespace-pre-line text-xs text-critical-700">
                {errors.filingDeadlineDays}
              </p>
            )}
          </section>

          <section className="border-t border-slate-100 pt-6">
            <PolicySwitch
              label={t("organization.requireApprovalBeforeAReport")}
              checked={draft.requireFilingApproval}
              disabled={saving}
              onChange={(next) => update({ requireFilingApproval: next })}
            >
              Submitting a report raises a four-eyes request instead of approving
              it outright. The report freezes at pending approval — its narrative
              can no longer be edited — until somebody other than the person who
              submitted it answers. A rejection sends it back to draft, and its
              deadline runs throughout.
            </PolicySwitch>
          </section>

          <section className="space-y-4 border-t border-slate-100 pt-6">
            <div>
              <h2 className="font-mono text-sm font-medium text-slate-900">
                {t("organization.whereTheMoneyIsIn")}
              </h2>
              <p className="mt-1 max-w-prose text-sm text-slate-500">
                A goAML report has to state an amount and a currency, and every
                organisation&apos;s payload puts them somewhere different. These
                two paths are how a filing finds them — dot and bracket notation,
                the same as everywhere else in this console.
              </p>
            </div>

            <PathField
              label={t("organization.amountPath")}
              value={draft.amountPath}
              placeholder={t("organization.paymentAmount")}
              error={errors.amountPath}
              disabled={saving}
              fields={schemaFields}
              onChange={(next) => {
                update({ amountPath: next });
                clearError("amountPath");
              }}
            />

            <PathField
              label={t("organization.currencyPath")}
              value={draft.currencyPath}
              placeholder={t("organization.paymentCurrency")}
              error={errors.currencyPath}
              disabled={saving}
              fields={schemaFields}
              onChange={(next) => {
                update({ currencyPath: next });
                clearError("currencyPath");
              }}
            />
          </section>
        </CardContent>
      </Card>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <Button className="gap-1.5" onClick={handleSave} disabled={saving}>
          <Save className="size-4" />
          {saving ? "Saving..." : "Save policy"}
        </Button>

        <p className="flex items-start gap-2 text-sm text-attention-700">
          <TriangleAlert className="mt-0.5 size-4 flex-shrink-0" />
          <span className="max-w-prose">
            Nothing here moves a deadline that already exists. Every{" "}
            <Link href={FILINGS_HREF} className="underline">
              filing already open
            </Link>{" "}
            keeps the deadline it was given when its case opened, including the
            ones that are already late.
          </span>
        </p>
      </div>
    </div>
  );
}

function PathField({
  label,
  value,
  placeholder,
  error,
  disabled,
  fields,
  onChange,
}: {
  label: string;
  value: string;
  placeholder: string;
  error: string | null | undefined;
  disabled: boolean;
  fields: Parameters<typeof SchemaFieldCombobox>[0]["fields"];
  onChange: (next: string) => void;
}) {
  const wildcard = value.trim().length > 0 && hasWildcard(value.trim());

  return (
    <div className="max-w-md space-y-1.5">
      <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
      </label>
      <SchemaFieldCombobox
        value={value}
        onChange={onChange}
        disabled={disabled}
        fields={fields}
        placeholder={placeholder}
        tone={error ? "critical" : wildcard ? "warning" : "default"}
      />
      {wildcard && (
        <p className="text-xs text-attention-700">
          Wildcards are refused here. A report states one amount and one
          currency, so the path has to resolve to a single value — name the
          element you mean, as in{" "}
          <code className="font-mono">parties[0].amount</code>.
        </p>
      )}
      {error && (
        <p className="whitespace-pre-line text-xs text-critical-700">{error}</p>
      )}
    </div>
  );
}
