"use client";

import { ReactNode, useState } from "react";
import Link from "next/link";

import { Rule, RuleEvaluation } from "@/types";
import { relationText } from "@/lib/resources";
import { MatchPolarityBadge } from "@/components/MatchPolarityBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { ConditionTable } from "@/components/evidence/ConditionTable";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function RuleEvidence({
  evaluation,
  showRule = true,
  showMatched = true,
  defaultOpen = false,
  actions,
  children,
  payload,
  rule,
}: {
  evaluation: RuleEvaluation;
  showRule?: boolean;
  showMatched?: boolean;
  defaultOpen?: boolean;
  actions?: ReactNode;
  children?: ReactNode;
  payload?: Record<string, unknown> | null;
  rule?: Rule;
}) {
  const t = useTranslation();
  const [open, setOpen] = useState(defaultOpen);

  const { ruleId, ruleVersion, conditionMatches } = evaluation;

  const hasTrace = conditionMatches !== null && conditionMatches.length > 0;

  const policyDrifted =
    rule !== undefined &&
    (rule.action !== ruleVersion.action ||
      rule.severity !== ruleVersion.severity ||
      rule.riskWeight !== ruleVersion.riskWeight);

  return (
    <div className="rounded-md border border-slate-200 bg-card p-4">
      <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          {showRule &&
            (rule?.isDeleted ? (
              <span className="text-sm text-slate-400">
                {relationText(rule?.friendlyName, ruleId)} (Deleted)
              </span>
            ) : (
              <Link
                href={`/rules/${ruleId}`}
                className="text-sm text-brand-800 hover:underline"
              >
                {relationText(rule?.friendlyName, ruleId)}
              </Link>
            ))}
          <span
            className="font-mono text-[11px] text-slate-500"
            title={t("evidence.theVersionThatJudgedThis")}
          >
            v{ruleVersion.versionNumber}
          </span>
          <StatusBadge status={ruleVersion.action} />
          <SeverityBadge severity={ruleVersion.severity} />
          {ruleVersion.riskWeight > 0 && (
            <span
              className="font-mono text-[11px] text-slate-400"
              title={t("evidence.contributionToTheRiskScore")}
            >
              +{ruleVersion.riskWeight}
            </span>
          )}
          <MatchPolarityBadge matchPolarity={ruleVersion.matchPolarity} />
        </div>

        <div className="flex flex-shrink-0 items-center gap-2">
          {showMatched && (
            <StatusBadge
              status={evaluation.matched ? "Pending" : "Approved"}
              label={evaluation.matched ? "Matched" : "Cleared"}
            />
          )}
          {hasTrace && (
            <Button
              variant="ghost"
              size="sm"
              className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
              onClick={() => setOpen((previous) => !previous)}
            >
              {open ? "Hide conditions" : "Why it fired"}
            </Button>
          )}
          {actions}
        </div>
      </div>

      <p className="mt-1 text-xs text-slate-400">
        <LocalTime value={evaluation.createdAt} />
      </p>

      {policyDrifted && (
        <p className="mt-2 text-xs text-slate-500">
          The rule now reads{" "}
          <span className="font-mono">{rule.action}</span> ·{" "}
          <span className="font-mono">{rule.severity}</span>
          {rule.riskWeight > 0 && (
            <span className="font-mono"> · +{rule.riskWeight}</span>
          )}
          . This transaction was judged on v{ruleVersion.versionNumber}&apos;s
          policy above.
        </p>
      )}

      {hasTrace && open && (
        <div className="mt-3">
          <ConditionTable
            conditionMatches={conditionMatches}
            ruleVersion={ruleVersion}
            payload={payload}
          />
        </div>
      )}

      {children}
    </div>
  );
}
