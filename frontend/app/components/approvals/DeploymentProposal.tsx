"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowRight } from "lucide-react";

import {
  ApprovalResource,
  ListRuleVersion,
  RuleDeployment,
  RuleVersion,
} from "@/types";
import { API_BASE } from "@/lib/api";
import { shortId } from "@/lib/resources";
import { cn } from "@/lib/utils";
import RuleBuilder from "@/components/RuleBuilder";
import { SeverityBadge } from "@/components/SeverityBadge";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import {
  DEPLOYMENT_STATUS_LABELS,
  STRATEGY_LABELS,
  versionLabel,
} from "@/lib/deployments";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function DeploymentProposal({
  resource,
}: {
  resource: ApprovalResource;
}) {
  const t = useTranslation();
  const { approval, ruleDeployment: deployment, rule } = resource;

  const [versions, setVersions] = useState<Record<string, RuleVersion>>({});
  const ruleId = deployment?.ruleId;

  useEffect(() => {
    if (!ruleId) return;

    let live = true;

    void (async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/rules/${ruleId}/versions?pageNumber=1&pageSize=100`,
          { credentials: "include" }
        );
        if (!response.ok || !live) return;

        const page = (await response.json()) as ListRuleVersion;
        if (!live) return;

        setVersions(
          Object.fromEntries(
            page.data.map((version) => [version.ruleVersionId, version])
          )
        );
      } catch {
      }
    })();

    return () => {
      live = false;
    };
  }, [ruleId]);

  const versionIndex = useMemo(
    () =>
      Object.fromEntries(
        Object.values(versions).map((version) => [
          version.ruleVersionId,
          version.versionNumber,
        ])
      ),
    [versions]
  );

  if (!deployment || !rule) {
    return (
      <div className="space-y-2 rounded-lg border border-dashed border-slate-200 bg-card p-4">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          Deployment{" "}
          {approval.ruleDeploymentId
            ? shortId(approval.ruleDeploymentId)
            : "—"}
        </p>
        <p className="max-w-prose text-sm text-slate-500">
          The rollout behind this request was not disclosed to you, so what it
          would deploy cannot be shown here. Everything else about the request —
          who raised it, when, and where it stands — is above.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-5 rounded-lg border border-slate-200 bg-card p-4">
      <div className="flex flex-wrap items-center gap-x-6 gap-y-3">
        <Detail label={t("common.rule")}>
          <Link
            href={`/rules/${rule.ruleId}`}
            className="text-sm text-brand-800 hover:underline"
          >
            {rule.friendlyName}
          </Link>
        </Detail>
        <Detail label={t("approvals.versionChange")}>
          <span className="inline-flex items-center gap-1.5 text-sm text-slate-900">
            <span className="text-slate-500">
              {versionLabel(deployment.previousRuleVersionId, versionIndex)}
            </span>
            <ArrowRight className="size-3.5 text-slate-400" />
            <span className="font-medium">
              {versionLabel(deployment.targetRuleVersionId, versionIndex)}
            </span>
          </span>
        </Detail>
        <Detail label={t("approvals.strategy")}>
          <span className="text-sm text-slate-900">
            {STRATEGY_LABELS[deployment.strategy] ?? deployment.strategy}
          </span>
        </Detail>
        <Detail label={t("approvals.deploymentStatus")}>
          <StatusBadge
            status={deployment.status}
            label={DEPLOYMENT_STATUS_LABELS[deployment.status]}
          />
        </Detail>
      </div>

      <p className="max-w-prose text-sm text-slate-600">
        {deployment.strategy === "Gradual" &&
        deployment.stepPercentage !== null &&
        deployment.stepIntervalSeconds !== null ? (
          <>
            Approving starts a gradual rollout: traffic shifts by{" "}
            <span className="font-mono">{deployment.stepPercentage}%</span> every{" "}
            <span className="font-mono">
              {deployment.stepIntervalSeconds}s
            </span>
            , and the live rule only flips to the target version once the rollout
            reaches 100%.
          </>
        ) : (
          <>
            Approving switches the live rule to the target version immediately,
            all at once.
          </>
        )}
      </p>

      <p className="font-mono text-xs text-slate-400">
        Currently at {deployment.currentPercentage}% — no traffic has reached the
        target version.
      </p>

      <VersionChange
        deployment={deployment}
        versions={versions}
        ruleId={rule.ruleId}
      />
    </div>
  );
}

function VersionChange({
  deployment,
  versions,
  ruleId,
}: {
  deployment: RuleDeployment;
  versions: Record<string, RuleVersion>;
  ruleId: string;
}) {
  const t = useTranslation();
  const [showLogic, setShowLogic] = useState(false);

  const target = versions[deployment.targetRuleVersionId];
  const isFirstDeployment = deployment.previousRuleVersionId === null;
  const previous = deployment.previousRuleVersionId
    ? versions[deployment.previousRuleVersionId]
    : undefined;

  if (!target || (!isFirstDeployment && !previous)) {
    return (
      <Section>
        <p className="max-w-prose text-sm text-slate-500">
          The versions behind this change could not be read here, so what it
          alters is not shown. Compare them on the{" "}
          <Link
            href={`/rules/${ruleId}`}
            className="text-brand-800 hover:underline"
          >
            rule&apos;s versions
          </Link>{" "}
          before answering.
        </p>
      </Section>
    );
  }

  const policyChanges = previous
    ? [
        {
          label: "Action",
          changed: previous.action !== target.action,
          before: <StatusBadge status={previous.action} />,
          after: <StatusBadge status={target.action} />,
        },
        {
          label: "Severity",
          changed: previous.severity !== target.severity,
          before: <SeverityBadge severity={previous.severity} emphasis />,
          after: <SeverityBadge severity={target.severity} emphasis />,
        },
        {
          label: t("rules.riskWeight"),
          changed: previous.riskWeight !== target.riskWeight,
          before: <Weight value={previous.riskWeight} />,
          after: <Weight value={target.riskWeight} />,
        },
      ].filter((row) => row.changed)
    : [];

  const logicChanged = previous
    ? JSON.stringify(previous.ruleBody) !== JSON.stringify(target.ruleBody)
    : true;

  return (
    <Section>
      {isFirstDeployment ? (
        <p className="max-w-prose text-sm text-slate-600">
          This is the rule&apos;s first deployment — there is no live version to
          replace. Approving puts the logic below into production for the first
          time, doing what the policy beside it says.
        </p>
      ) : policyChanges.length === 0 && !logicChanged ? (
        <p className="max-w-prose text-sm text-slate-600">
          Nothing changes. The target version matches what is live, in both its
          policy and its logic — this is a redeploy of the same rule.
        </p>
      ) : (
        <>
          {policyChanges.length > 0 ? (
            <div className="space-y-2">
              <p className="text-sm text-slate-600">
                {t("approvals.whatFiringThisRuleDoes")}
              </p>
              <dl className="space-y-1.5">
                {policyChanges.map((row) => (
                  <div
                    key={row.label}
                    className="flex flex-wrap items-center gap-x-3 gap-y-1"
                  >
                    <dt className="w-24 flex-shrink-0 font-mono text-xs uppercase tracking-widest text-slate-400">
                      {row.label}
                    </dt>
                    <dd className="flex items-center gap-2">
                      <span className="opacity-60">{row.before}</span>
                      <ArrowRight className="size-3.5 text-slate-400" />
                      {row.after}
                    </dd>
                  </div>
                ))}
              </dl>
            </div>
          ) : (
            <p className="text-sm text-slate-600">
              What firing this rule does is unchanged — same action, severity and
              risk weight.
            </p>
          )}

          <div className="space-y-2">
            <p className="text-sm text-slate-600">
              {logicChanged
                ? "What the rule matches changes."
                : "What the rule matches is unchanged."}
            </p>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowLogic((open) => !open)}
            >
              {showLogic ? "Hide the logic" : "Show what the logic does"}
            </Button>
          </div>

          {showLogic && (
            <div
              className={cn(
                "grid gap-4",
                previous && logicChanged ? "md:grid-cols-2" : "grid-cols-1"
              )}
            >
              {previous && logicChanged && (
                <LogicPanel
                  label={`Live now — ${versionLabelOf(previous)}`}
                  version={previous}
                  emphasise={false}
                />
              )}
              <LogicPanel
                label={
                  logicChanged
                    ? `Would go live — ${versionLabelOf(target)}`
                    : `Unchanged — ${versionLabelOf(target)}`
                }
                version={target}
                emphasise
              />
            </div>
          )}
        </>
      )}
    </Section>
  );
}

const versionLabelOf = (version: RuleVersion): string =>
  `v${version.versionNumber}`;

function LogicPanel({
  label,
  version,
  emphasise,
}: {
  label: string;
  version: RuleVersion;
  emphasise: boolean;
}) {
  return (
    <div className="space-y-1.5">
      <p
        className={cn(
          "text-xs font-medium",
          emphasise ? "text-slate-900" : "text-slate-500"
        )}
      >
        {label}
      </p>
      <div className="rounded-lg border border-slate-200 bg-card p-3">
        <RuleBuilder
          key={version.ruleVersionId}
          ruleSetId={version.ruleSetId}
          initialRules={[version.ruleBody]}
          submitRuleText=""
          readOnly
          matchPolarity={version.matchPolarity}
        />
      </div>
    </div>
  );
}

function Weight({ value }: { value: number }) {
  return (
    <span className="font-mono text-sm text-slate-900">
      {value > 0 ? `+${value}` : value}
    </span>
  );
}

function Section({ children }: { children: React.ReactNode }) {
  const t = useTranslation();
  return (
    <div className="space-y-3 border-t border-slate-100 pt-4">
      <h3 className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {t("approvals.whatChangesIfYouApprove")}
      </h3>
      {children}
    </div>
  );
}

function Detail({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="min-w-0 space-y-1">
      <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
      </p>
      {children}
    </div>
  );
}
