"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { JsonEditor } from "@/components/JsonEditor";
import { PayloadForm } from "@/components/evaluate/PayloadForm";
import { Play, Wand2 } from "lucide-react";

import {
  ApplicationUser,
  Decision,
  EvaluateAllRulesResource,
  EvaluateAllRulesResponse,
  EvaluateRuleResource,
  EvaluateRuleResponse,
  EvaluateRuleSetResource,
  EvaluateRuleSetResponse,
  Paged,
  Policy,
  ProblemDetails,
  Rule,
  RuleAction,
  RuleResource,
  RuleSet,
  RuleSetResource,
  SandboxCreditResponse,
  SchemaViolation,
  ValidationProblemDetails,
  Outcome
} from "@/types";
import { useAuth } from "@/context/AuthContext";
import { useEnvironment } from "@/context/ModeContext";
import { usePermissions } from "@/context/PermissionsContext";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { DecisionBadgeWithReason } from "@/components/DecisionBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import {
  API_BASE,
  formatSchemaViolations,
  formatValidationErrors,
  readSchemaViolations,
  unwrap,
} from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { samplePayloadText } from "@/lib/transactionSchema";
import { useTransactionSchema } from "@/lib/useTransactionSchema";
import {
  EvaluateError,
  EvaluateResult,
  IdempotencyKeyReusedError,
  InvalidIdempotencyKeyError,
  RateLimitedError,
  TransactionReferenceConflictError,
  evaluateRule,
  evaluateRuleSet,
  evaluateRuleSets
} from "@/lib/idempotency";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";

import { apiFetch } from "@/lib/http";
import { subscribeToSandboxCredit } from "@/lib/sandbox";
import { useTranslation } from "@/context/LocalizationContext";
import { useFormat } from "@/lib/useFormat";

type Target = "ruleSet" | "rule" | "all";

interface EvalRelations {
  rules?: Rule[];
  users?: ApplicationUser[];
  ruleSet?: RuleSet;
}

type EvalResult = EvalRelations &
  (
    | { kind: "rule"; data: EvaluateRuleResponse }
    | { kind: "ruleSet"; data: EvaluateRuleSetResponse }
    | { kind: "all"; data: EvaluateAllRulesResponse }
  );

const ACTION_ORDER: RuleAction[] = ["Allow", "Flag", "Review", "Block"];
const actionRank = (a: RuleAction) => ACTION_ORDER.indexOf(a);

const opensCase = (action: RuleAction) => action === "Review" || action === "Block";

const firedPolicies = (results: EvaluateRuleResponse[]): Policy[] =>
  results.filter((r) => r.matched).map((r) => r.policy);

const maxFiredAction = (policies: Policy[]): RuleAction =>
  policies.reduce<RuleAction>(
    (max, p) => (actionRank(p.action) > actionRank(max) ? p.action : max),
    "Allow"
  );

const isEscalated = (outcome: Outcome, policies: Policy[]): boolean =>
  actionRank(outcome.action) > actionRank(maxFiredAction(policies));

const DEFAULT_PAYLOAD = "{\n  \n}";

const isBlankPayload = (text: string): boolean => {
  const stripped = text.replace(/\s/g, "");
  return stripped === "" || stripped === "{}";
};

const readPayloadObject = (text: string): Record<string, unknown> | null => {
  try {
    const parsed: unknown = JSON.parse(text);
    return typeof parsed === "object" && parsed !== null && !Array.isArray(parsed)
      ? (parsed as Record<string, unknown>)
      : null;
  } catch {
    return null;
  }
};

export default function EvaluatePage() {
  const t = useTranslation();
  const format = useFormat();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const { mode } = useEnvironment();
  const [sandboxCredit, setSandboxCredit] = useState<SandboxCreditResponse | null>(null);
  const loadSandboxCredit = useCallback(async () => {
    if (mode !== "test") { setSandboxCredit(null); return; }
    try {
      const response = await apiFetch(`${API_BASE}/organizations/my/sandbox/credits`);
      if (response.ok) setSandboxCredit(await unwrap<SandboxCreditResponse>(response));
    } catch {
    }
  }, [mode]);
  useEffect(() => { void loadSandboxCredit(); }, [loadSandboxCredit]);
  useEffect(() => subscribeToSandboxCredit(() => { void loadSandboxCredit(); }), [loadSandboxCredit]);
  const { holds, holdsAny, resolved } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const canEvaluateRules = holds("rules:evaluate");
  const canEvaluateRuleSets = holds("rule-sets:evaluate");

  useEffect(() => {
    if (!resolved) return;
    if (!holdsAny("rules:evaluate", "rule-sets:evaluate")) {
      toast.error(
        t("evaluate.runningRuleNeedsRulesEvaluate")
      );
      router.push("/");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolved, canEvaluateRules, canEvaluateRuleSets]);

  const [target, setTarget] = useState<Target>("ruleSet");

  const targetOptions = (
    [
      ["ruleSet", "One rule set", canEvaluateRuleSets],
      ["rule", "One rule", canEvaluateRules],
      ["all", "All rule sets", canEvaluateRuleSets]
    ] as [Target, string, boolean][]
  )
    .filter(([, , held]) => held)
    .map(([value, label]) => [value, label] as [Target, string]);

  useEffect(() => {
    if (!resolved || targetOptions.length === 0) return;
    if (!targetOptions.some(([value]) => value === target)) {
      setTarget(targetOptions[0][0]);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolved, canEvaluateRules, canEvaluateRuleSets, target]);

  const [ruleSetSearch, setRuleSetSearch] = useState<string>("");
  const [ruleSetOptions, setRuleSetOptions] = useState<RuleSet[]>([]);
  const [ruleSetDropdownOpen, setRuleSetDropdownOpen] = useState(false);
  const [selectedRuleSet, setSelectedRuleSet] = useState<RuleSet | null>(null);

  const [ruleSearch, setRuleSearch] = useState<string>("");
  const [ruleOptions, setRuleOptions] = useState<Rule[]>([]);
  const [ruleDropdownOpen, setRuleDropdownOpen] = useState(false);
  const [selectedRule, setSelectedRule] = useState<Rule | null>(null);

  const [payloadText, setPayloadText] = useState<string>(DEFAULT_PAYLOAD);
  const [transactionReference, setTransactionReference] = useState<string>("");
  const [createCase, setCreateCase] = useState<boolean>(false);
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [result, setResult] = useState<EvalResult | null>(null);
  const [replayed, setReplayed] = useState<boolean | null>(null);
  const submission = useIdempotencyKey();
  const [violations, setViolations] = useState<SchemaViolation[]>([]);
  const [caseRequested, setCaseRequested] = useState<boolean>(false);

  const { fields: schemaFields } = useTransactionSchema(
    holds("transaction-schemas:read")
  );

  const fillFromSchema = useCallback(() => {
    if (!schemaFields?.length) return;
    setPayloadText(samplePayloadText(schemaFields));
  }, [schemaFields]);

  const [payloadView, setPayloadView] = useState<"form" | "json">("form");
  const payloadObject = useMemo(
    () => readPayloadObject(payloadText),
    [payloadText]
  );
  const hasSchema = Boolean(schemaFields?.length);
  const showForm = payloadView === "form" && hasSchema && payloadObject !== null;

  const prefilled = useRef(false);

  useEffect(() => {
    if (prefilled.current || !schemaFields?.length) return;
    prefilled.current = true;
    setPayloadText((current) =>
      isBlankPayload(current) ? samplePayloadText(schemaFields) : current
    );
  }, [schemaFields]);

  const fetchRuleSets = useCallback(
    async (search: string) => {
      try {
        const response = await apiFetch(
          `${API_BASE}/rule-sets?FriendlyName=${encodeURIComponent(search)}`,
          { credentials: "include" }
        );
        if (response.ok) {
          const data = (await response.json()) as Paged<RuleSetResource>;
          setRuleSetOptions(data.data.map((resource) => resource.ruleSet));
        } else if (response.status === 401) {
          goToLogin();
        }
      } catch {
      }
    },
    [goToLogin]
  );

  const fetchRules = useCallback(
    async (ruleSetId: string, search: string) => {
      try {
        const response = await apiFetch(
          `${API_BASE}/rule-sets/${ruleSetId}/rules?FriendlyName=${encodeURIComponent(search)}`,
          { credentials: "include" }
        );
        if (response.ok) {
          const data = (await response.json()) as Paged<RuleResource>;
          setRuleOptions(data.data.map((resource) => resource.rule));
        } else if (response.status === 401) {
          goToLogin();
        }
      } catch {
      }
    },
    [goToLogin]
  );

  useEffect(() => {
    if (target === "ruleSet" || target === "rule") {
      fetchRuleSets(ruleSetSearch);
    }
  }, [target, ruleSetSearch, fetchRuleSets]);

  useEffect(() => {
    if (target === "rule" && selectedRuleSet) {
      fetchRules(selectedRuleSet.ruleSetId, ruleSearch);
    }
  }, [target, selectedRuleSet, ruleSearch, fetchRules]);

  const pickRuleSet = (ruleSet: RuleSet) => {
    setSelectedRuleSet(ruleSet);
    setRuleSetSearch(ruleSet.friendlyName);
    setRuleSetDropdownOpen(false);
    setSelectedRule(null);
    setRuleSearch("");
  };

  const pickRule = (rule: Rule) => {
    setSelectedRule(rule);
    setRuleSearch(rule.friendlyName);
    setRuleDropdownOpen(false);
  };

  const canSubmit =
    target === "all" ||
    (target === "ruleSet" && selectedRuleSet !== null) ||
    (target === "rule" && selectedRule !== null);

  const runEvaluation = async () => {
    let payload: unknown;
    try {
      payload = JSON.parse(payloadText);
    } catch {
      toast.error(t("common.payloadInvalidJson"));
      return;
    }

    setSubmitting(true);
    setResult(null);
    setReplayed(null);
    setViolations([]);
    setCaseRequested(createCase);

    const options = {
      idempotencyKey: submission.current(),
      transactionReference: transactionReference.trim() || undefined,
      createCase
    };

    try {
      if (target === "rule" && selectedRule) {
        const res: EvaluateResult<EvaluateRuleResource> = await evaluateRule(
          selectedRule.ruleId,
          payload,
          options
        );
        setResult({
          kind: "rule",
          data: res.data.result,
          rules: res.data.rules,
          users: res.data.users,
        });
        setReplayed(res.replayed);
        submission.completed();
        void loadSandboxCredit();
      } else if (target === "ruleSet" && selectedRuleSet) {
        const res: EvaluateResult<EvaluateRuleSetResource> = await evaluateRuleSet(
          selectedRuleSet.ruleSetId,
          payload,
          options
        );
        setResult({
          kind: "ruleSet",
          data: res.data.result,
          rules: res.data.rules,
          users: res.data.users,
          ruleSet: res.data.ruleSet,
        });
        setReplayed(res.replayed);
        submission.completed();
        void loadSandboxCredit();
      } else if (target === "all") {
        const res: EvaluateResult<EvaluateAllRulesResource> = await evaluateRuleSets(
          payload,
          options
        );
        setResult({
          kind: "all",
          data: res.data.result,
          rules: res.data.rules,
          users: res.data.users,
        });
        setReplayed(res.replayed);
        submission.completed();
        void loadSandboxCredit();
      }
    } catch (err) {
      if (err instanceof RateLimitedError) {
        toast.error(mode === "test" ? t("common.sandboxEvaluationLimitReached") : t("common.apiBusy"), {
          description: err.retryAfterSeconds
            ? t(
                "evaluate.rateLimitRetryIn",
                t(
                  mode === "test"
                    ? "evaluate.sharedLimitReached"
                    : "evaluate.rateLimitReached"
                ),
                err.retryAfterSeconds
              )
            : t(
                "evaluate.rateLimitRetryShortly",
                t(
                  mode === "test"
                    ? "evaluate.sharedLimitReached"
                    : "evaluate.rateLimitReached"
                )
              ),
          action: mode === "test" ? { label: t("evaluate.sandboxSettings"), onClick: () => router.push("/organization#Sandbox") } : undefined,
        });
      } else if (
        err instanceof IdempotencyKeyReusedError ||
        err instanceof TransactionReferenceConflictError ||
        err instanceof InvalidIdempotencyKeyError
      ) {
        toast.error(err.message);
      } else if (err instanceof EvaluateError) {
        const schema = readSchemaViolations(err.body);
        if (err.response.status === 401) {
          toast.error(t("common.pleaseLoginPerformAction"));
          goToLogin();
        } else if (err.response.status === 402) {
        } else if (err.response.status === 429) {
          const retry = err.response.headers.get("Retry-After");
          toast.error(t("common.sandboxEvaluationLimitReached"), {
            description: retry ? t("evaluate.sharedLimitRetryIn", retry) : t("common.shared120RequestsMinuteLimit"),
            action: { label: t("evaluate.sandboxSettings"), onClick: () => router.push("/organization#Sandbox") },
          });
        } else if (schema) {
          setViolations(schema.violations);
          toast.error(schema.title, {
            description: formatSchemaViolations(schema.violations),
            descriptionClassName: "whitespace-pre-line",
          });
        } else if (
          err.response.status === 400 &&
          (err.body as ValidationProblemDetails | null)?.errors
        ) {
          toast.error(
            formatValidationErrors(err.body as ValidationProblemDetails)
          );
        } else {
          toast.error(
            (err.body as ProblemDetails | null)?.detail || t("evaluate.evaluationFailed")
          );
        }
      } else {
        toast.error(t("evaluate.failedRunEvaluation"));
      }
    } finally {
      setSubmitting(false);
    }
  };

  const policies: Policy[] =
    result === null
      ? []
      : result.kind === "rule"
        ? result.data.matched
          ? [result.data.policy]
          : []
        : firedPolicies(result.data.results);

  const outcome: Outcome | null = result === null ? null : result.data.outcome;

  const decision: Decision | null = result === null ? null : result.data.decision;

  const escalated = outcome !== null && isEscalated(outcome, policies);
  const reviewThreshold =
    result?.kind === "ruleSet" ? (result.ruleSet?.reviewThreshold ?? 100) : null;

  const rows: EvaluateRuleResponse[] =
    result === null ? [] : result.kind === "rule" ? [result.data] : result.data.results;

  return (
    <div className="space-y-6">
      {mode === "test" && <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-attention-300 bg-attention-50 px-3 py-2 text-sm text-attention-900"><span>{t("evaluate.sandboxEvaluationOneRulePass")}</span>{sandboxCredit && <span className="font-mono text-xs">{t("evaluate.creditsRemaining", format.count(sandboxCredit.remaining), format.count(sandboxCredit.allowance))}</span>}</div>}
      <PageHeader
        eyebrow={t("evaluate.rules")}
        title={t("evaluate.evaluate")}
        description={t("evaluate.runAPayloadThroughThe")}
      >
        {user?.organization && (
          <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {user.organization.friendlyName}
          </span>
        )}
      </PageHeader>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="space-y-4">
          <div className="rounded-lg border border-slate-200 bg-card p-4 space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("evaluate.target")}
              </label>
              <div className="flex gap-1.5">
                {targetOptions.map(([value, label]) => (
                  <Button
                    key={value}
                    type="button"
                    size="sm"
                    variant={target === value ? "default" : "outline"}
                    onClick={() => {
                      setTarget(value);
                      setResult(null);
                    }}
                  >
                    {label}
                  </Button>
                ))}
              </div>
            </div>

            {(target === "ruleSet" || target === "rule") && (
              <div className="relative space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("evaluate.ruleSet")}
                </label>
                <Input
                  placeholder={t("evaluate.searchRuleSets")}
                  value={ruleSetSearch}
                  onChange={(e) => {
                    setRuleSetSearch(e.target.value);
                    setSelectedRuleSet(null);
                  }}
                  onFocus={() => setRuleSetDropdownOpen(true)}
                  onBlur={() => setTimeout(() => setRuleSetDropdownOpen(false), 200)}
                />
                {ruleSetDropdownOpen && ruleSetOptions.length > 0 && (
                  <ul className="absolute left-0 right-0 z-20 mt-1 max-h-48 overflow-y-auto rounded-md border border-slate-200 bg-card shadow-md">
                    {ruleSetOptions.map((rs) => (
                      <li
                        key={rs.ruleSetId}
                        className="cursor-pointer px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                        onMouseDown={() => pickRuleSet(rs)}
                      >
                        {rs.friendlyName}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            {target === "rule" && selectedRuleSet && (
              <div className="relative space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.rule")}
                </label>
                <Input
                  placeholder={t("evaluate.searchRulesInThisSet")}
                  value={ruleSearch}
                  onChange={(e) => {
                    setRuleSearch(e.target.value);
                    setSelectedRule(null);
                  }}
                  onFocus={() => setRuleDropdownOpen(true)}
                  onBlur={() => setTimeout(() => setRuleDropdownOpen(false), 200)}
                />
                {ruleDropdownOpen && ruleOptions.length > 0 && (
                  <ul className="absolute left-0 right-0 z-20 mt-1 max-h-48 overflow-y-auto rounded-md border border-slate-200 bg-card shadow-md">
                    {ruleOptions.map((r) => (
                      <li
                        key={r.ruleId}
                        className="cursor-pointer px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                        onMouseDown={() => pickRule(r)}
                      >
                        {r.friendlyName}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            <div className="space-y-1.5">
              <div className="flex items-center justify-between gap-2">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("evaluate.payload")}
                </label>
                {hasSchema && (
                  <div className="flex items-center gap-1">
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      className="h-6 gap-1.5 px-2 text-xs"
                      onClick={fillFromSchema}
                    >
                      <Wand2 className="size-3.5" />
                      {t("evaluate.fillFromSchema")}
                    </Button>
                    {(["form", "json"] as const).map((view) => (
                      <Button
                        key={view}
                        type="button"
                        size="sm"
                        variant={payloadView === view ? "default" : "outline"}
                        className="h-6 px-2 text-xs"
                        disabled={view === "form" && payloadObject === null}
                        title={
                          view === "form" && payloadObject === null
                            ? t("evaluate.payloadNotJsonObject")
                            : undefined
                        }
                        onClick={() => setPayloadView(view)}
                      >
                        {view === "form" ? "Form" : "JSON"}
                      </Button>
                    ))}
                  </div>
                )}
              </div>

              {showForm ? (
                <div className="max-h-[420px] overflow-y-auto rounded-md border border-slate-200 p-3">
                  <PayloadForm
                    fields={schemaFields!}
                    value={payloadObject!}
                    onChange={(next) =>
                      setPayloadText(JSON.stringify(next, null, 2))
                    }
                    violations={violations}
                  />
                </div>
              ) : (
                <div className="rounded-md border border-slate-200 overflow-hidden">
                  <JsonEditor
                    value={payloadText}
                    onChange={setPayloadText}
                    height="220px"
                  />
                </div>
              )}

              {hasSchema && (
                <p className="text-xs text-slate-400">
                  {showForm
                    ? "Built from this organisation's transaction schema. A field marked null is not sent, which is what the schema reads as absent — switch to JSON to add fields it does not declare."
                    : "Prefilled from this organisation's transaction schema. Required fields carry a placeholder of the declared type; optional ones are null, which the schema reads as absent. Fields it does not declare are still allowed — add them yourself."}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("evaluate.transactionReferenceOptional")}
              </label>
              <Input
                placeholder={t("evaluate.yourOwnIdentifierForThis")}
                value={transactionReference}
                onChange={(e) => setTransactionReference(e.target.value)}
              />
              {mode === "test" && (
                <div className="rounded-md border border-attention-200 bg-attention-50/60 p-2.5">
                  <p className="mb-1.5 text-xs text-attention-800">
                    {t("evaluate.useADeterministicTestTrigger")}
                  </p>
                  <div className="flex flex-wrap gap-1">
                    {(["allow", "flag", "review", "hold", "block", "error"] as const).map((trigger) => (
                      <Button key={trigger} type="button" size="sm" variant="ghost" className="h-6 px-1.5 font-mono text-[11px] text-attention-800" onClick={() => setTransactionReference(`tm_test_${trigger}`)}>
                        tm_test_{trigger}
                      </Button>
                    ))}
                  </div>
                </div>
              )}
              <p className="text-xs text-slate-400">
                Reuse the same reference across separate evaluations of one
                transaction to land them on the same record. Leave blank and
                the platform mints a name for you.
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.case")}
              </label>
              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  checked={createCase}
                  onChange={(e) => setCreateCase(e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 accent-brand-800"
                />
                <span className="text-sm text-slate-700">
                  {t("evaluate.openACaseWhenThe")}
                </span>
              </div>
              <p className="text-xs text-slate-400">
                {t("evaluate.sends")} <code className="font-mono">{t("evaluate.xCreateCase")}</code>.{" "}
                {t("evaluate.itAlsoDecidesWhatA")}
              </p>
            </div>

            <Button
              className="gap-1.5"
              disabled={!canSubmit || submitting}
              onClick={runEvaluation}
            >
              <Play className="size-4" />
              {submitting ? "Evaluating..." : "Evaluate"}
            </Button>
          </div>
        </div>

        <div className="space-y-4">
          {violations.length > 0 && (
            <div className="rounded-lg border border-critical-200 bg-critical-50 p-4">
              <div className="font-mono text-xs uppercase tracking-widest text-critical-700">
                {t("evaluate.payloadRejected")}
              </div>
              <p className="mt-1 text-sm text-critical-700">
                This payload does not match the organisation&apos;s transaction
                schema, so nothing was evaluated and no transaction was
                recorded.
              </p>
              <ul className="mt-2 space-y-1 text-xs text-critical-700">
                {violations.map((violation, index) => (
                  <li key={index}>
                    <span className="font-mono">{violation.field}</span>{" "}
                    {violation.message}
                  </li>
                ))}
              </ul>
              <p className="mt-2 text-xs text-critical-700/80">
                Fields the schema does not declare are allowed and are never
                listed here — only what it declares and the payload got wrong.
              </p>
            </div>
          )}

          {result === null ? (
            violations.length === 0 && (
              <div className="flex h-full min-h-64 items-center justify-center rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-6 text-center text-sm text-slate-400">
                Run an evaluation to see the resolved outcome and per-rule
                breakdown here.
              </div>
            )
          ) : (
            <>
              {replayed && (
                <div className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
                  This is a replayed response — the idempotency key matched an
                  evaluation already recorded, so nothing new happened.
                </div>
              )}

              {outcome && (
                <div className="rounded-lg border border-slate-200 bg-card p-4">
                  <div className="mb-2 font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("evaluate.resolvedOutcome")}
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusBadge status={outcome.action} />
                    <SeverityBadge severity={outcome.severity} />
                    <span className="font-mono text-sm text-slate-600">
                      risk score {outcome.riskScore}
                    </span>
                  </div>
                  {escalated && (
                    <p className="mt-2 text-xs text-inflight-700">
                      Escalated to Review: risk score {outcome.riskScore}
                      {reviewThreshold !== null ? ` of ${reviewThreshold}` : ""}
                      . No individual fired rule asked for Review or higher —
                      the combined risk score crossed the threshold.
                    </p>
                  )}
                </div>
              )}

              {decision && (
                <div className="rounded-lg border border-slate-200 bg-card p-4">
                  <div className="mb-2 font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("evaluate.decision")}
                  </div>
                  <DecisionBadgeWithReason
                    status={decision.status}
                    reasonCode={decision.reasonCode}
                  />
                  {decision.status === "Held" && (
                    <p className="mt-2 text-xs text-attention-700">
                      The transaction is held until an analyst resolves the
                      case below.
                    </p>
                  )}
                  <a
                    href={`/transactions/${decision.transactionId}`}
                    className="mt-2 inline-block text-xs text-brand-800 hover:underline"
                  >
                    {t("evaluate.openTheTransaction")}
                  </a>
                </div>
              )}

              <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-slate-50 hover:bg-slate-50">
                      <TableHead>{t("common.rule")}</TableHead>
                      <TableHead>{t("evaluate.fired")}</TableHead>
                      <TableHead>{t("evaluate.policy")}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rows.map((row) => (
                      <TableRow key={row.ruleId} className="hover:bg-slate-50/50">
                        <TableCell className="font-medium">
                          <Link
                            href={`/rules/${row.ruleId}`}
                            className="text-brand-800 hover:underline"
                          >
                            {row.friendlyName}
                          </Link>
                        </TableCell>
                        <TableCell>
                          <StatusBadge
                            status={row.matched ? "Enabled" : "Disabled"}
                            label={row.matched ? "Fired" : t("evaluate.didNotFire")}
                          />
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1.5">
                            <StatusBadge status={row.policy.action} />
                            <SeverityBadge severity={row.policy.severity} />
                            {row.policy.riskWeight > 0 && (
                              <span className="font-mono text-[11px] text-slate-400">
                                +{row.policy.riskWeight}
                              </span>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              {result.data.case ? (
                <div className="rounded-md border border-inflight-200 bg-inflight-50/60 px-3 py-2 text-xs text-inflight-700">
                  Opened case{" "}
                  <a
                    href={`/cases/${result.data.case.caseId}`}
                    className="font-medium underline"
                  >
                    {result.data.case.friendlyName}
                  </a>
                </div>
              ) : (
                decision?.caseId ? (
                  <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
                    Already decided on an earlier evaluation — no new case was
                    opened.{" "}
                    <a
                      href={`/cases/${decision.caseId}`}
                      className="font-medium text-brand-800 underline"
                    >
                      {t("evaluate.openTheOriginalCase")}
                    </a>
                  </div>
                ) : (
                  !caseRequested &&
                  outcome !== null &&
                  opensCase(outcome.action) && (
                    <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
                      No case was opened: this run did not ask for one. Tick
                      &ldquo;Open a case&rdquo; and evaluate again — under a new
                      transaction reference, since a decided transaction keeps
                      the decision it already has.
                    </div>
                  )
                )
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
