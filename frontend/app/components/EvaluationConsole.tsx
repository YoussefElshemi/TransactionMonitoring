"use client";

import { useEffect, useState } from "react";
import { useTranslation } from "@/context/LocalizationContext";

type Outcome = "true" | "false" | "skipped";

interface Condition {
  field: string;
  operator: string;
  value: string;
  outcome: Outcome;
}

interface Scenario {
  reference: string;
  payload: [string, string][];
  ruleSet: string;
  rule: string;
  conditions: Condition[];
  verdict: { matched: boolean; headlineKey: string; detailKey: string };
}

const scenarios: Scenario[] = [
  {
    reference: "txn_8f21c4",
    payload: [
      ["amount", "9400.00"],
      ["currency", '"GBP"'],
      ["beneficiaryAgeHours", "4"],
      ["beneficiaryAllowlisted", "false"],
      ["corridor", '"GB-NG"'],
    ],
    ruleSet: "Outbound payments",
    rule: "High value to a new beneficiary",
    conditions: [
      { field: "amount", operator: ">", value: "5000", outcome: "true" },
      {
        field: "beneficiaryAgeHours",
        operator: "<",
        value: "24",
        outcome: "true",
      },
      {
        field: "beneficiaryAllowlisted",
        operator: "=",
        value: "false",
        outcome: "true",
      },
    ],
    verdict: {
      matched: true,
      headlineKey: "evaluate.exampleCaseOpened",
      detailKey: "evaluate.examplePendingAnalyst",
    },
  },
  {
    reference: "txn_8f21c5",
    payload: [
      ["amount", "82.50"],
      ["currency", '"GBP"'],
      ["beneficiaryAgeHours", "9310"],
      ["beneficiaryAllowlisted", "true"],
      ["corridor", '"GB-GB"'],
    ],
    ruleSet: "Outbound payments",
    rule: "High value to a new beneficiary",
    conditions: [
      { field: "amount", operator: ">", value: "5000", outcome: "false" },
      {
        field: "beneficiaryAgeHours",
        operator: "<",
        value: "24",
        outcome: "skipped",
      },
      {
        field: "beneficiaryAllowlisted",
        operator: "=",
        value: "false",
        outcome: "skipped",
      },
    ],
    verdict: {
      matched: false,
      headlineKey: "evaluate.exampleCleared",
      detailKey: "evaluate.exampleShortCircuited",
    },
  },
];

const usePrefersReducedMotion = () => {
  const [reduced, setReduced] = useState(false);

  useEffect(() => {
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    setReduced(query.matches);
    const listener = (event: MediaQueryListEvent) => setReduced(event.matches);
    query.addEventListener("change", listener);
    return () => query.removeEventListener("change", listener);
  }, []);

  return reduced;
};

const outcomeLabel: Record<Outcome, string> = {
  true: "true",
  false: "false",
  skipped: "skipped",
};

function ConditionRow({
  condition,
  isLast,
  resolved,
}: {
  condition: Condition;
  isLast: boolean;
  resolved: boolean;
}) {
  const tone = !resolved
    ? "text-console-600"
    : condition.outcome === "true"
      ? "text-attention-500"
      : condition.outcome === "false"
        ? "text-console-400"
        : "text-console-600";

  return (
    <div className="flex items-baseline gap-2 py-[3px]">
      <span className="select-none text-console-700">{isLast ? "└" : "├"}</span>
      <span className="text-console-300">{condition.field}</span>
      <span className="text-brand-400">{condition.operator}</span>
      <span className="text-console-300">{condition.value}</span>
      <span
        className={`ml-auto pl-4 tabular-nums transition-opacity duration-300 ${tone} ${
          resolved ? "opacity-100" : "opacity-40"
        }`}
      >
        {resolved ? outcomeLabel[condition.outcome] : "·"}
      </span>
    </div>
  );
}

export default function EvaluationConsole() {
  const t = useTranslation();
  const reducedMotion = usePrefersReducedMotion();
  const [index, setIndex] = useState(0);
  const [step, setStep] = useState(0);

  const scenario = scenarios[index];
  const finalStep = scenario.conditions.length + 1;

  useEffect(() => {
    if (reducedMotion) {
      setStep(finalStep);
      return;
    }

    if (step < finalStep) {
      const delay = step === 0 ? 850 : 600;
      const timer = setTimeout(() => setStep((current) => current + 1), delay);
      return () => clearTimeout(timer);
    }

    const timer = setTimeout(() => {
      setIndex((current) => (current + 1) % scenarios.length);
      setStep(0);
    }, 3600);
    return () => clearTimeout(timer);
  }, [step, index, reducedMotion, finalStep]);

  const verdictShown = step >= finalStep;

  return (
    <figure className="m-0">
      <div className="overflow-hidden rounded-xl border border-console-border bg-console shadow-2xl shadow-console/20">
        <div className="flex items-center justify-between border-b border-console-border px-4 py-2.5">
          <span className="font-mono text-[11px] text-console-400">
            {t("evaluationConsole.postRuleSetsEvaluate")}
          </span>
          <span className="font-mono text-[11px] text-console-600">
            {scenario.reference}
          </span>
        </div>

        <div className="overflow-x-auto px-4 py-4 font-mono text-[11px] leading-relaxed sm:text-xs">
          <div className="min-w-[19rem]">
            <p className="mb-2 uppercase tracking-widest text-console-600">
              payload
            </p>
            {scenario.payload.map(([key, value]) => (
              <div key={key} className="flex gap-2 py-[2px]">
                <span className="text-console-500">{key}</span>
                <span className="ml-auto text-console-200">{value}</span>
              </div>
            ))}

            <div className="my-4 border-t border-dashed border-console-border" />

            <p className="uppercase tracking-widest text-console-600">
              {scenario.ruleSet}
            </p>
            <p className="mb-2 mt-1 text-console-300">{scenario.rule}</p>
            <p className="text-brand-400">and</p>
            {scenario.conditions.map((condition, position) => (
              <ConditionRow
                key={condition.field}
                condition={condition}
                isLast={position === scenario.conditions.length - 1}
                resolved={step > position}
              />
            ))}
          </div>
        </div>

        <div
          className={`flex items-center gap-3 border-t px-4 py-3 font-mono text-[11px] transition-all duration-500 sm:text-xs ${
            verdictShown
              ? "opacity-100"
              : "pointer-events-none opacity-0"
          } ${
            scenario.verdict.matched
              ? "border-attention-500/25 bg-attention-500/10"
              : "border-settled-500/25 bg-settled-500/10"
          }`}
        >
          <span
            className={`size-1.5 flex-shrink-0 rounded-full ${
              scenario.verdict.matched ? "bg-attention-500" : "bg-settled-500"
            }`}
          />
          <span
            className={
              scenario.verdict.matched ? "text-attention-500" : "text-settled-500"
            }
          >
            {t(scenario.verdict.headlineKey)}
          </span>
          <span className="ml-auto truncate pl-3 text-console-500">
            {t(scenario.verdict.detailKey)}
          </span>
        </div>
      </div>

      <figcaption className="mt-3 text-center font-mono text-[11px] text-console-400">
        {t("evaluationConsole.illustrativeEvaluation")}
      </figcaption>
    </figure>
  );
}
