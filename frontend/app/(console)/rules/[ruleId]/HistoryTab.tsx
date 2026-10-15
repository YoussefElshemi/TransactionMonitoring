"use client";

import { useState } from "react";

import {
  AuditLogChange,
  RuleBodyDto,
  RuleExpressionDto,
  RuleMatchPolarity,
} from "@/types";
import RuleBuilder from "@/components/RuleBuilder";
import { HistoryPanel, historyPath } from "@/components/history";
import { TextDiff } from "@/components/history";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

interface HistoryTabProps {
  ruleId: string;
  matchPolarity: RuleMatchPolarity;
  onUnauthorized: () => void;
}

const RULE_BODY_FIELD = "RuleBody";

const picker =
  (obj: Record<string, unknown>) =>
  (pascal: string, camel: string): unknown =>
    obj[pascal] !== undefined ? obj[pascal] : obj[camel];

const normaliseExpression = (raw: unknown): RuleExpressionDto | null => {
  if (raw === null || typeof raw !== "object") return null;
  const pick = picker(raw as Record<string, unknown>);

  const args = pick("Arguments", "arguments");

  return {
    field: (pick("Field", "field") as string | null) ?? null,
    value: pick("Value", "value") ?? null,
    function: (pick("Function", "function") as RuleExpressionDto["function"]) ?? null,
    arguments: Array.isArray(args)
      ? args
          .map(normaliseExpression)
          .filter((item): item is RuleExpressionDto => item !== null)
      : null,
  };
};

// Snapshot rule bodies use PascalCase keys (Condition/Field/Quantifier/
// Expression/Value/And/Or/Not), straight from the DB entity — but
// RuleBuilder/RuleBodyDto expect camelCase. Normalise recursively, tolerating
// either casing.
const normaliseRuleBody = (raw: unknown): RuleBodyDto | null => {
  if (raw === null || typeof raw !== "object") return null;
  const obj = raw as Record<string, unknown>;
  const pick = picker(obj);
  const list = (v: unknown): RuleBodyDto[] | null =>
    Array.isArray(v)
      ? v
          .map((item) => normaliseRuleBody(item))
          .filter((item): item is RuleBodyDto => item !== null)
      : null;
  return {
    condition: (pick("Condition", "condition") as RuleBodyDto["condition"]) ?? null,
    field: (pick("Field", "field") as string | null) ?? null,
    quantifier:
      (pick("Quantifier", "quantifier") as RuleBodyDto["quantifier"]) ?? null,
    expression: normaliseExpression(pick("Expression", "expression")),
    value: pick("Value", "value") ?? null,
    and: list(pick("And", "and")),
    or: list(pick("Or", "or")),
    not: list(pick("Not", "not")),
    json: (pick("Json", "json") as boolean | undefined) ?? false,
  };
};

const parseRuleBody = (value: unknown): RuleBodyDto | null => {
  if (value === null || value === undefined) return null;
  let raw: unknown = value;
  if (typeof value === "string") {
    try {
      raw = JSON.parse(value);
    } catch {
      return null;
    }
  }
  return normaliseRuleBody(raw);
};

function RuleBodyPanel({
  label,
  value,
  emphasise,
  matchPolarity,
}: {
  label: string;
  value: unknown;
  emphasise: boolean;
  matchPolarity: RuleMatchPolarity;
}) {
  const body = parseRuleBody(value);
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
      {body ? (
        <div className="rounded-lg border border-slate-200 bg-card p-3">
          <RuleBuilder
            key={JSON.stringify(value)}
            ruleSetId=""
            initialRules={[body]}
            submitRuleText=""
            readOnly
            matchPolarity={matchPolarity}
          />
        </div>
      ) : (
        <div className="rounded-lg border border-dashed border-slate-200 bg-card p-3 text-sm text-slate-400">
          {value === null || value === undefined ? "None" : "Unparseable rule body"}
        </div>
      )}
    </div>
  );
}

const pretty = (value: unknown): string | null => {
  if (value === null || value === undefined) return null;
  if (typeof value !== "string") return JSON.stringify(value, null, 2);
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
};

function RuleBodyChange({
  change,
  isInitialSnapshot,
  matchPolarity,
}: {
  change: AuditLogChange;
  isInitialSnapshot: boolean;
  matchPolarity: RuleMatchPolarity;
}) {
  const t = useTranslation();
  const [showJson, setShowJson] = useState<boolean>(false);

  return (
    <div className="space-y-3">
      {isInitialSnapshot ? (
        <RuleBodyPanel
          label={t("rules.initialLogic")}
          value={change.newValue}
          emphasise
          matchPolarity={matchPolarity}
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <RuleBodyPanel
            label={t("rules.before")}
            value={change.oldValue}
            emphasise={false}
            matchPolarity={matchPolarity}
          />
          <RuleBodyPanel
            label={t("rules.after")}
            value={change.newValue}
            emphasise
            matchPolarity={matchPolarity}
          />
        </div>
      )}

      <Button
        variant="ghost"
        size="sm"
        className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
        onClick={() => setShowJson((open) => !open)}
      >
        {showJson ? "Hide JSON diff" : "Show JSON diff"}
      </Button>

      {showJson && (
        <TextDiff
          before={isInitialSnapshot ? null : pretty(change.oldValue)}
          after={pretty(change.newValue)}
          previewLines={12}
        />
      )}
    </div>
  );
}

export function HistoryTab({
  ruleId,
  matchPolarity,
  onUnauthorized,
}: HistoryTabProps) {
  return (
    <HistoryPanel
      entityType="Rule"
      path={historyPath.Rule(ruleId)}
      entityId={ruleId}
      onUnauthorized={onUnauthorized}
      renderChange={(change, { isInitialSnapshot }) =>
        change.field === RULE_BODY_FIELD ? (
          <RuleBodyChange
            change={change}
            isInitialSnapshot={isInitialSnapshot}
            matchPolarity={matchPolarity}
          />
        ) : null
      }
    />
  );
}
