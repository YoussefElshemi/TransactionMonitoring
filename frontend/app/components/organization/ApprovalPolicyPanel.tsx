"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Save, ShieldCheck, TriangleAlert, X } from "lucide-react";

import {
  ApprovalPolicy,
  ApprovalPolicyBody,
  RuleBodyDto,
  ValidationProblemDetails,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { PolicySwitch } from "@/components/PolicySwitch";
import RuleBuilder from "@/components/RuleBuilder";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const NOT_SET = "NotSet";

const SEVERITY_ERROR_KEY = "Body.CaseDecisionMinimumSeverity";
const MATCHER_ERROR_PREFIX = "Body.CaseDecisionMatcher";

interface PolicyDraft {
  requireDeploymentApproval: boolean;
  caseDecisionMinimumSeverity: string | null;
  caseDecisionMatcher: RuleBodyDto | null;
}

const EMPTY_DRAFT: PolicyDraft = {
  requireDeploymentApproval: false,
  caseDecisionMinimumSeverity: null,
  caseDecisionMatcher: null,
};

const draftFrom = (policy: ApprovalPolicy): PolicyDraft => ({
  requireDeploymentApproval: policy.requireDeploymentApproval,
  caseDecisionMinimumSeverity: policy.caseDecisionMinimumSeverity,
  caseDecisionMatcher: policy.caseDecisionMatcher,
});

const policyBody = (draft: PolicyDraft): ApprovalPolicyBody => ({
  requireDeploymentApproval: draft.requireDeploymentApproval,
  caseDecisionMinimumSeverity: draft.caseDecisionMinimumSeverity,
  caseDecisionMatcher: draft.caseDecisionMatcher,
});

export function ApprovalPolicyPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [draft, setDraft] = useState<PolicyDraft>(EMPTY_DRAFT);
  const [stated, setStated] = useState<boolean | null>(null);
  const [drafting, setDrafting] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [severityError, setSeverityError] = useState<string | null>(null);
  const [matcherError, setMatcherError] = useState<string | null>(null);
  // Remounts the rule builder, which owns its own tree and is not a controlled
  // input — the same device the queue matcher uses.
  const [matcherEpoch, setMatcherEpoch] = useState(0);

  const fetchPolicy = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/approval-policy`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const policy = await unwrap<ApprovalPolicy>(response);
        setDraft(draftFrom(policy));
        setStated(true);
        setDrafting(false);
        return;
      }

      if (response.status === 404) {
        setDraft(EMPTY_DRAFT);
        setStated(false);
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.errorOccurredFetchingApprovalPolicy"),
        onUnauthorized,
        toastId: "error-fetching-approval-policy",
      });
    } catch {
      toast.error(t("organization.errorOccurredFetchingApprovalPolicy"), {
        id: "error-fetching-approval-policy",
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

  const handleSave = async () => {
    setSaving(true);
    setSeverityError(null);
    setMatcherError(null);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/approval-policy`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(policyBody(draft)),
        }
      );

      if (response.ok) {
        toast.success(t("organization.approvalPolicySaved"), {
          description: gatesNothing(draft)
            ? t("organization.nothingGatedDeploymentsCaseDecisions")
            : t("organization.appliesFromNowRequestsAlready"),
        });
        await fetchPolicy();
        return;
      }

      if (response.status === 400) {
        const body = (await response.json().catch(() => null)) as
          | ValidationProblemDetails
          | null;
        const errors = body?.errors ?? {};

        const severity = errors[SEVERITY_ERROR_KEY];
        if (severity?.length) setSeverityError(severity.join("\n"));

        const matcher = Object.entries(errors)
          .filter(([key]) => key.startsWith(MATCHER_ERROR_PREFIX))
          .flatMap(([, messages]) => messages);
        if (matcher.length) setMatcherError(matcher.join("\n"));

        if (!severity?.length && matcher.length === 0) {
          toast.error(body?.detail || t("organization.approvalPolicyRefused"));
        }
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedSaveApprovalPolicy"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedSaveApprovalPolicy"));
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Card>
        <CardContent className="space-y-3 pt-6">
          {Array.from({ length: 4 }).map((_, row) => (
            <Skeleton key={row} className="h-10 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  if (stated === false && !drafting) {
    return (
      <Card>
        <CardContent className="space-y-4 py-12 text-center">
          <ShieldCheck className="mx-auto size-8 text-slate-300" />
          <div className="space-y-2">
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("organization.noApprovalPolicy")}
            </h2>
            <p className="mx-auto max-w-prose text-sm text-slate-500">
              This organisation gates nothing. Anyone who can deploy a rule
              deploys it, and anyone who can close a case closes it — which is
              how every organisation starts.
            </p>
            <p className="mx-auto max-w-prose text-sm text-slate-500">
              A policy makes the person who proposes an action somebody other
              than the person who signs it off. It never grants anybody a
              permission; it only ever holds an action back for a second pair of
              eyes.
            </p>
          </div>
          <Button className="gap-1.5" onClick={() => setDrafting(true)}>
            <ShieldCheck className="size-4" />
            {t("organization.setUpApprovalPolicy")}
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardContent className="space-y-6 pt-6">
          <section className="space-y-3">
            <PolicySwitch
              label={t("organization.requireApprovalToDeployA")}
              checked={draft.requireDeploymentApproval}
              disabled={saving}
              onChange={(next) => update({ requireDeploymentApproval: next })}
            >
              No author deploys their own rule. Deploying writes a proposal
              instead and waits — the target version reaches no traffic at all
              until somebody holding approvals:decide, who is not the author, agrees to it.
            </PolicySwitch>
          </section>

          <section className="space-y-4 border-t border-slate-100 pt-6">
            <div>
              <h2 className="font-mono text-sm font-medium text-slate-900">
                {t("organization.caseDecisionsThatNeedA")}
              </h2>
              <p className="mt-1 max-w-prose text-sm text-slate-500">
                {t("organization.caseConditionsAreEitherOf")}
              </p>
              <p className="mt-2 max-w-prose text-sm text-slate-500">
                {t("organization.leaveBothUnsetToGate")}
              </p>
            </div>

            <div className="max-w-sm space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("organization.severityFloor")}
              </label>
              <Select
                value={draft.caseDecisionMinimumSeverity ?? NOT_SET}
                onValueChange={(next) => {
                  update({
                    caseDecisionMinimumSeverity: next === NOT_SET ? null : next,
                  });
                  setSeverityError(null);
                }}
                disabled={saving}
              >
                <SelectTrigger
                  aria-invalid={severityError ? true : undefined}
                  className={
                    severityError
                      ? "border-critical-500 focus:ring-critical-500"
                      : undefined
                  }
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NOT_SET}>
                    {t("organization.notSetSeverityGatesNothing")}
                  </SelectItem>
                  {SEVERITY_FILTERS.map((severity) => (
                    <SelectItem key={severity} value={severity}>
                      {severity} and above
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                Inclusive: a floor of High also holds back Critical cases. A case
                takes the severity of the worst rule that fired when it opened.
              </p>
              {severityError && (
                <p className="whitespace-pre-line text-xs text-critical-700">
                  {severityError}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between gap-3">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("organization.matcher")}
                </label>
                {draft.caseDecisionMatcher && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="gap-1.5 text-slate-500 hover:text-slate-900"
                    disabled={saving}
                    onClick={() => {
                      update({ caseDecisionMatcher: null });
                      setMatcherError(null);
                      setMatcherEpoch((epoch) => epoch + 1);
                    }}
                  >
                    <X className="size-4" />
                    {t("organization.removeMatcher")}
                  </Button>
                )}
              </div>

              {draft.caseDecisionMatcher ? (
                <div className="space-y-2 rounded-lg border border-slate-200 p-3">
                  <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
                    Editing here is separate from the policy. Press{" "}
                    <span className="font-medium">{t("organization.useThisMatcher")}</span> to
                    bring changes back to the form, then Save the policy to keep
                    them.
                  </p>
                  <RuleBuilder
                    key={matcherEpoch}
                    ruleSetId=""
                    initialRules={[draft.caseDecisionMatcher]}
                    submitRuleText={t("organization.useThisMatcher")}
                    saveButtonText={t("organization.useThisMatcher")}
                    readOnly={false}
                    allowVelocity={false}
                    handleSaveBody={(rules) => {
                      update({ caseDecisionMatcher: rules[0] ?? null });
                      setMatcherError(null);
                      toast.success(t("common.matcherUpdated"), {
                        description: t("organization.savePolicyKeep"),
                      });
                    }}
                  />
                  {matcherError && (
                    <p className="whitespace-pre-line text-xs text-critical-700">
                      {matcherError}
                    </p>
                  )}
                </div>
              ) : (
                <div className="rounded-lg border border-dashed border-slate-200 px-4 py-6 text-center">
                  <p className="mx-auto max-w-prose text-sm text-slate-500">
                    No matcher. Add one to hold decisions back on the payment
                    itself — an amount above a threshold, a country, a
                    party&apos;s risk score.
                  </p>
                  <Button
                    variant="outline"
                    size="sm"
                    className="mt-3"
                    disabled={saving}
                    onClick={() => {
                      update({
                        caseDecisionMatcher: {
                          condition: null,
                          field: null,
                          value: null,
                        } as RuleBodyDto,
                      });
                      setMatcherEpoch((epoch) => epoch + 1);
                    }}
                  >
                    {t("organization.addAMatcher")}
                  </Button>
                  {matcherError && (
                    <p className="mt-2 whitespace-pre-line text-xs text-critical-700">
                      {matcherError}
                    </p>
                  )}
                </div>
              )}
            </div>
          </section>

          {gatesNothing(draft) && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
              As it stands this policy gates nothing: no deployment approval, no
              severity floor and no matcher. Saving it is how an organisation
              turns off conditions it stated earlier.
            </p>
          )}
        </CardContent>
      </Card>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <Button className="gap-1.5" onClick={handleSave} disabled={saving}>
          <Save className="size-4" />
          {saving ? "Saving..." : "Save policy"}
        </Button>
        {stated === false && (
          <Button
            variant="outline"
            disabled={saving}
            onClick={() => {
              setDraft(EMPTY_DRAFT);
              setDrafting(false);
              setSeverityError(null);
              setMatcherError(null);
            }}
          >
            {t("common.cancel")}
          </Button>
        )}
        <p className="flex items-start gap-2 text-sm text-attention-700">
          <TriangleAlert className="mt-0.5 size-4 flex-shrink-0" />
          <span className="max-w-prose">
            Relaxing or turning off a condition leaves requests already pending
            exactly where they are. They still need answering — a case with an
            open request keeps refusing to close even after the policy that held
            it back is gone.
          </span>
        </p>
      </div>
    </div>
  );
}

const gatesNothing = (draft: PolicyDraft): boolean =>
  !draft.requireDeploymentApproval &&
  draft.caseDecisionMinimumSeverity === null &&
  draft.caseDecisionMatcher === null;
