"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { toast } from "sonner";

import { useAuth } from "@/context/AuthContext";
import {
  HoldPolicy,
  HoldPolicyFields,
  holdPolicyPatch,
} from "@/components/HoldPolicyFields";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { API_BASE, reportApiError } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const FIELD_LABELS: Record<string, string> = {
  "Body.HoldWindow": "Hold window",
  "Body.HoldExpiryVerdict": "When the hold expires",
};

const FIELD_OF: Record<string, keyof HoldPolicy> = {
  "Body.HoldWindow": "holdWindow",
  "Body.HoldExpiryVerdict": "holdExpiryVerdict",
};

export function HoldPolicyPanel() {
  const t = useTranslation();
  const { user, refreshUser } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const organization = user?.organization;

  const saved: HoldPolicy = {
    holdWindow: organization?.holdWindow ?? null,
    holdExpiryVerdict: organization?.holdExpiryVerdict ?? null,
  };

  const [policy, setPolicy] = useState<HoldPolicy>(saved);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<keyof HoldPolicy, string>>
  >({});

  useEffect(() => {
    setPolicy({
      holdWindow: organization?.holdWindow ?? null,
      holdExpiryVerdict: organization?.holdExpiryVerdict ?? null,
    });
  }, [organization?.holdWindow, organization?.holdExpiryVerdict]);

  const patch = holdPolicyPatch(policy, saved);
  const dirty = Object.keys(patch).length > 0;

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
        `${API_BASE}/organizations/my/hold-policy`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(patch),
          credentials: "include",
        }
      );

      if (response.ok) {
        toast.success(t("common.holdPolicyUpdated"));
        await refreshUser();
        return;
      }

      if (response.status === 400) {
        const body = await response.clone().json().catch(() => null);
        const errors = (body as { errors?: Record<string, string[]> } | null)
          ?.errors;
        if (errors) {
          const next: Partial<Record<keyof HoldPolicy, string>> = {};
          for (const [key, messages] of Object.entries(errors)) {
            const field = FIELD_OF[key];
            if (field) next[field] = messages.join(" ");
          }
          setFieldErrors(next);
        }
      }

      await reportApiError(response, {
        fallback: t("organization.failedUpdateHoldPolicy"),
        onUnauthorized: () => router.push(loginHref(redirectUri)),
        fieldLabels: FIELD_LABELS,
      });
    } catch {
      toast.error(t("organization.failedUpdateHoldPolicy"));
    } finally {
      setSaving(false);
    }
  };

  if (!organization) return null;

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">{t("organization.holdPolicy")}</h2>
          <p className="mt-0.5 text-sm text-slate-500">
            The default for every rule in this organization that does not set
            its own. A rule&apos;s own policy always wins, and where several
            rules fire together the longest window and the most restrictive
            verdict win.
          </p>
        </div>

        <HoldPolicyFields
          scope="organization"
          value={policy}
          onChange={(next) => {
            setPolicy(next);
            setFieldErrors({});
          }}
          disabled={saving}
          inheritance={{ organization: null }}
          errors={fieldErrors}
        />

        <div className="flex items-center gap-2 border-t border-slate-100 pt-4">
          <Button size="sm" onClick={save} disabled={!dirty || saving}>
            {saving ? "Saving..." : "Save hold policy"}
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

        <p className="text-xs text-slate-400">
          Changing this never moves a deadline that is already running — a hold
          keeps the policy it started with, so a transaction someone has
          already been given a date for is unaffected.
        </p>
      </CardContent>
    </Card>
  );
}
