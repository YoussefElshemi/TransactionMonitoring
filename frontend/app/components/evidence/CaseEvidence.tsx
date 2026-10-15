import { Rule, RuleEvaluation } from "@/types";
import { severityRank } from "@/lib/severity";
import { RuleEvidence } from "@/components/evidence/RuleEvidence";
import { useTranslation } from "@/context/LocalizationContext";

export function CaseEvidence({
  ruleEvaluations,
  payload,
  rules,
}: {
  ruleEvaluations: RuleEvaluation[];
  payload?: Record<string, unknown> | null;
  rules?: ReadonlyMap<string, Rule>;
}) {
  const t = useTranslation();
  const ordered = [...ruleEvaluations].sort(
    (left, right) =>
      severityRank(right.ruleVersion.severity) -
      severityRank(left.ruleVersion.severity)
  );

  return (
    <div>
      <h2 className="font-mono text-sm font-medium tracking-tight text-slate-900">
        {t("evidence.whyThisFired")}
      </h2>
      <p className="mb-3 mt-0.5 text-sm text-slate-500">
        {ordered.length === 1
          ? "The rule that fired on this transaction, and the values behind it."
          : `The ${ordered.length} rules that fired on this transaction, and the values behind each.`}
      </p>
      <div className="space-y-2">
        {ordered.map((evaluation) => (
          <RuleEvidence
            key={evaluation.ruleEvaluationId}
            evaluation={evaluation}
            payload={payload}
            rule={rules?.get(evaluation.ruleId)}
            showMatched={false}
            defaultOpen
          />
        ))}
      </div>
    </div>
  );
}
