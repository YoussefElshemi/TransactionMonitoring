"use client";

import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { ApplicationUser, Decision, Rule } from "@/types";
import {
  isSystemExpiry,
  isTerminalDecision,
  reasonText,
  statusText,
} from "@/lib/decisions";
import { relationText, userName } from "@/lib/resources";
import { DecisionBadge } from "@/components/DecisionBadge";
import { HoldCountdown, HoldOutcome } from "@/components/HoldExpiry";
import { SeverityBadge } from "@/components/SeverityBadge";
import { StatusBadge } from "@/components/StatusBadge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function DecisionPanel({
  decision,
  showCaseLink = true,
  transactionHref,
  showFiredRules = true,
  showHold = true,
  rules,
  users,
}: {
  decision: Decision | null;
  showCaseLink?: boolean;
  transactionHref?: string;
  showFiredRules?: boolean;
  showHold?: boolean;
  rules?: ReadonlyMap<string, Rule>;
  users?: ReadonlyMap<string, ApplicationUser>;
}) {
  const t = useTranslation();
  if (!decision) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">{t("decisionPanel.decision")}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-slate-500">
            {t("decisionPanel.noVerdictHasBeenRecorded")}
          </p>
        </CardContent>
      </Card>
    );
  }

  const meaning = statusText(decision.status);
  const terminal = isTerminalDecision(decision.status);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center gap-3 text-base font-semibold">
          {t("decisionPanel.decision")}
          <DecisionBadge status={decision.status} />
          {decision.status === "Held" && (
            <span className="font-mono text-[11px] uppercase tracking-widest text-attention-700">
              {t("decisionPanel.awaitingAnAnalyst")}
            </span>
          )}
        </CardTitle>
      </CardHeader>

      <CardContent className="space-y-4 text-sm">
        <div>
          <p className="text-slate-700">{reasonText(decision.reasonCode)}</p>
          {meaning && <p className="mt-0.5 text-slate-500">{meaning}</p>}
        </div>

        {showHold && (
          <>
            <HoldCountdown decision={decision} />
            <HoldOutcome decision={decision} />
          </>
        )}

        <div className="grid grid-cols-2 gap-x-8 gap-y-3">
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("decisionPanel.reasonCode")}</span>
            <p className="mt-0.5 break-all font-mono text-slate-900">
              {decision.reasonCode}
            </p>
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("decisionPanel.decision")}</span>
            <p className="mt-0.5 break-all font-mono text-xs text-slate-500">
              {decision.decisionId}
            </p>
          </div>

          <div>
            <span className="font-medium text-slate-500">{t("decisionPanel.held")}</span>
            {decision.heldAt ? (
              <p className="mt-0.5 text-slate-900">
                <LocalTime value={decision.heldAt} />
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">{t("decisionPanel.neverHeld")}</p>
            )}
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("decisionPanel.resolved")}</span>
            {decision.resolvedAt ? (
              <p className="mt-0.5 text-slate-900">
                <LocalTime value={decision.resolvedAt} />
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                {terminal ? "Not recorded" : "Still open"}
              </p>
            )}
          </div>

          <div>
            <span className="font-medium text-slate-500">{t("decisionPanel.holdDeadline")}</span>
            {decision.expiresAt ? (
              <p className="mt-0.5 text-slate-900">
                <LocalTime value={decision.expiresAt} />
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                {decision.holdExpiryVerdict === "Never"
                  ? "None — waits for a decision"
                  : decision.heldAt
                    ? "None"
                    : "Never held"}
              </p>
            )}
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("decisionPanel.onExpiry")}</span>
            {decision.holdExpiryVerdict ? (
              <p className="mt-0.5 font-mono text-slate-900">
                {decision.holdExpiryVerdict}
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">{t("decisionPanel.notApplicable")}</p>
            )}
          </div>

          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("decisionPanel.resolvedBy")}</span>
            {decision.resolvedByUserId ? (
              <p className="mt-0.5 break-all text-slate-900">
                {relationText(
                  userName(users?.get(decision.resolvedByUserId)),
                  decision.resolvedByUserId
                )}
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                {isSystemExpiry(decision)
                  ?
                    "Nobody — the hold expired"
                  : terminal
                    ? "The rules, with no analyst involved"
                    : "Nobody yet"}
              </p>
            )}
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("decisionPanel.yourReference")}</span>
            {decision.transactionReference ? (
              <p className="mt-0.5 break-all font-mono text-slate-900">
                {decision.transactionReference}
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">{t("decisionPanel.none")}</p>
            )}
          </div>
        </div>

        {(transactionHref || (showCaseLink && decision.caseId)) && (
          <div className="flex flex-wrap items-center gap-4">
            {transactionHref && (
              <Link
                href={transactionHref}
                className="inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
              >
                {t("decisionPanel.theTransactionThisDecides")}
                <ArrowRight className="size-3.5" />
              </Link>
            )}
            {showCaseLink && decision.caseId && (
              <Link
                href={`/cases/${decision.caseId}`}
                className="inline-flex items-center gap-1.5 text-sm text-brand-800 hover:underline"
              >
                {t("decisionPanel.theCaseThatHoldsIt")}
                <ArrowRight className="size-3.5" />
              </Link>
            )}
          </div>
        )}

        {showFiredRules && <FiredRules decision={decision} rules={rules} />}
      </CardContent>
    </Card>
  );
}

function FiredRules({
  decision,
  rules,
}: {
  decision: Decision;
  rules?: ReadonlyMap<string, Rule>;
}) {
  const t = useTranslation();
  return (
    <div className="border-t border-slate-100 pt-4">
      <span className="font-medium text-slate-500">{t("decisionPanel.rulesThatFired")}</span>
      {decision.firedRules.length === 0 ? (
        <p className="mt-1 text-sm text-slate-400">
          {t("decisionPanel.noneNothingThisTransactionWas")}
        </p>
      ) : (
        <ul className="mt-2 space-y-2">
          {decision.firedRules.map((fired) => (
            <li
              key={fired.ruleEvaluationId}
              className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-slate-200 bg-card px-3 py-2"
            >
              <div className="flex min-w-0 items-center gap-2">
                {rules?.get(fired.ruleId)?.isDeleted ? (
                  <span className="min-w-0 truncate text-sm text-slate-400">
                    {relationText(
                      rules?.get(fired.ruleId)?.friendlyName,
                      fired.ruleId
                    )}{" "}
                    (Deleted)
                  </span>
                ) : (
                  <Link
                    href={`/rules/${fired.ruleId}`}
                    className="min-w-0 truncate text-sm text-brand-800 hover:underline"
                  >
                    {relationText(
                      rules?.get(fired.ruleId)?.friendlyName,
                      fired.ruleId
                    )}
                  </Link>
                )}
                <span
                  className="flex-shrink-0 font-mono text-[11px] text-slate-500"
                  title={t("decisionPanel.theVersionThatJudgedThis")}
                >
                  v{fired.ruleVersion.versionNumber}
                </span>
              </div>
              <div className="flex flex-shrink-0 items-center gap-1.5">
                <StatusBadge status={fired.ruleVersion.action} />
                <SeverityBadge severity={fired.ruleVersion.severity} />
                {fired.ruleVersion.riskWeight > 0 && (
                  <span
                    className="font-mono text-[11px] text-slate-400"
                    title={t("decisionPanel.contributionToTheRiskScore")}
                  >
                    +{fired.ruleVersion.riskWeight}
                  </span>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
