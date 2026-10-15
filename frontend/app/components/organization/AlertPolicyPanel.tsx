"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { AlertTriangle } from "lucide-react";
import { toast } from "sonner";

import { useAuth } from "@/context/AuthContext";
import {
  AlertPolicy,
  AlertPolicyFields,
  EMPTY_ALERT_POLICY,
  alertPolicyPatch,
} from "@/components/AlertPolicyFields";
import { DEFAULT_CASE_GROUPING_ENABLED } from "@/lib/alertPolicy";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { API_BASE, reportApiError } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { Organization } from "@/types";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const FIELD_LABELS: Record<string, string> = {
  "Body.CaseGroupingEnabled": "Group alerts into cases",
  "Body.DeduplicationEnabled": "Suppress duplicate alerts",
  "Body.CaseGroupingPeriod": "Case grouping period",
  "Body.DeduplicationWindow": "Deduplication window",
  "Body.CaseGroupingScope": "Grouping scope",
  "Body.CaseGroupingSeverityPolicy": "Severity policy",
  "Body.MaxAlertsPerCase": "Max alerts per case",
};

const FIELD_OF: Record<string, keyof AlertPolicy> = {
  "Body.CaseGroupingEnabled": "caseGroupingEnabled",
  "Body.DeduplicationEnabled": "deduplicationEnabled",
  "Body.CaseGroupingPeriod": "caseGroupingPeriod",
  "Body.DeduplicationWindow": "deduplicationWindow",
  "Body.CaseGroupingScope": "caseGroupingScope",
  "Body.CaseGroupingSeverityPolicy": "caseGroupingSeverityPolicy",
  "Body.MaxAlertsPerCase": "maxAlertsPerCase",
};

export const alertPolicyOf = (
  organization: Pick<
    Organization,
    | "caseGroupingEnabled"
    | "deduplicationEnabled"
    | "caseGroupingPeriod"
    | "deduplicationWindow"
    | "caseGroupingScope"
    | "caseGroupingSeverityPolicy"
    | "maxAlertsPerCase"
  > | null
): AlertPolicy =>
  organization
    ? {
        caseGroupingEnabled: organization.caseGroupingEnabled ?? null,
        deduplicationEnabled: organization.deduplicationEnabled ?? null,
        caseGroupingPeriod: organization.caseGroupingPeriod ?? null,
        deduplicationWindow: organization.deduplicationWindow ?? null,
        caseGroupingScope: organization.caseGroupingScope ?? null,
        caseGroupingSeverityPolicy:
          organization.caseGroupingSeverityPolicy ?? null,
        maxAlertsPerCase: organization.maxAlertsPerCase ?? null,
      }
    : EMPTY_ALERT_POLICY;

export function AlertPolicyPanel({
  organizationSegment,
  saved,
  onSaved,
}: {
  organizationSegment: string;
  saved: AlertPolicy;
  onSaved: () => void | Promise<unknown>;
}) {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [policy, setPolicy] = useState<AlertPolicy>(saved);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<keyof AlertPolicy, string>>
  >({});

  useEffect(() => {
    setPolicy(saved);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    saved.caseGroupingEnabled,
    saved.deduplicationEnabled,
    saved.caseGroupingPeriod,
    saved.deduplicationWindow,
    saved.caseGroupingScope,
    saved.caseGroupingSeverityPolicy,
    saved.maxAlertsPerCase,
  ]);

  const patch = alertPolicyPatch(policy, saved);
  const dirty = Object.keys(patch).length > 0;

  const enablingGrouping =
    "caseGroupingEnabled" in patch &&
    policy.caseGroupingEnabled === true &&
    (saved.caseGroupingEnabled ?? DEFAULT_CASE_GROUPING_ENABLED) === false;

  const reset = () => {
    setPolicy(saved);
    setFieldErrors({});
  };

  const save = async () => {
    if (!dirty) return;

    setSaving(true);
    setFieldErrors({});

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/alert-policy`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          // `patch` carries only what changed, and carries an explicit null for
          // a field cleared back to the house default. Both matter: an omitted
          // key leaves the stored value alone, a null clears it, and spreading
          // form state here would turn all seven inherited values into
          // explicit ones. `JSON.stringify` keeps explicit nulls.
          body: JSON.stringify(patch),
          credentials: "include",
        }
      );

      if (response.ok) {
        toast.success(t("organization.alertPolicyUpdated"));
        await onSaved();
        return;
      }

      if (response.status === 400) {
        const body = await response.clone().json().catch(() => null);
        const errors = (body as { errors?: Record<string, string[]> } | null)
          ?.errors;
        if (errors) {
          const next: Partial<Record<keyof AlertPolicy, string>> = {};
          for (const [key, messages] of Object.entries(errors)) {
            const field = FIELD_OF[key];
            if (field) next[field] = messages.join(" ");
          }
          setFieldErrors(next);
        }
      }

      await reportApiError(response, {
        fallback: t("organization.failedUpdateAlertPolicy"),
        onUnauthorized: () => router.push(loginHref(redirectUri)),
        fieldLabels: FIELD_LABELS,
      });
    } catch {
      toast.error(t("organization.failedUpdateAlertPolicy"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">{t("organization.alertPolicy")}</h2>
          <p className="mt-0.5 max-w-prose text-sm text-slate-500">
            How this organization&apos;s alerts turn into work. Two separate
            questions: whether a finding is a repeat of one already raised, and
            whether it belongs to a case somebody already has open on the same
            party. Anything left at its default is not stored — it follows the
            house setting if that ever changes.
          </p>
        </div>

        <AlertPolicyFields
          value={policy}
          onChange={(next) => {
            setPolicy(next);
            setFieldErrors({});
          }}
          disabled={saving}
          errors={fieldErrors}
        />

        {enablingGrouping && (
          <div className="flex items-start gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2">
            <AlertTriangle className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <div className="max-w-prose text-xs text-attention-700">
              <p className="font-medium">
                {t("organization.savingThisChangesWhatYour")}
              </p>
              <p className="mt-0.5">
                From now on a transaction that would have opened its own case
                joins the party&apos;s open one instead, and is triaged at that
                case&apos;s severity. Cases already open are unaffected — this
                only decides where alerts raised after the change land.
              </p>
            </div>
          </div>
        )}

        <div className="flex items-center gap-2 border-t border-slate-100 pt-4">
          <Button size="sm" onClick={save} disabled={!dirty || saving}>
            {saving ? "Saving..." : "Save alert policy"}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={reset}
            disabled={!dirty || saving}
          >
            {t("common.reset")}
          </Button>
        </div>

        <p className="max-w-prose text-xs text-slate-400">
          Changing this never re-files alerts that have already been raised — a
          case keeps the alerts it was built with, and nothing already suppressed
          comes back.
        </p>
      </CardContent>
    </Card>
  );
}

export function MyAlertPolicyPanel() {
  const { user, refreshUser } = useAuth();
  const organization = user?.organization;

  if (!organization) return null;

  return (
    <AlertPolicyPanel
      organizationSegment="my"
      saved={alertPolicyOf(organization)}
      onSaved={refreshUser}
    />
  );
}
