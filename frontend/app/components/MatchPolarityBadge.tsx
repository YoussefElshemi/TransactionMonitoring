"use client";

import { RuleMatchPolarity } from "@/types";
import { cn } from "@/lib/utils";

import { useTranslation } from "@/context/LocalizationContext";

export const POLARITY_LABEL: Record<RuleMatchPolarity, string> = {
  MatchesWhenTrue: "polarity.firesWhenMet",
  MatchesWhenFalse: "polarity.firesWhenNotMet",
};

export const POLARITY_OPTION_LABEL: Record<RuleMatchPolarity, string> = {
  MatchesWhenTrue: "polarity.conditionIsMet",
  MatchesWhenFalse: "polarity.conditionIsNotMet",
};

export const POLARITY_OPTIONS: RuleMatchPolarity[] = [
  "MatchesWhenTrue",
  "MatchesWhenFalse",
];

export function MatchPolarityBadge({
  matchPolarity,
  className,
}: {
  matchPolarity: RuleMatchPolarity | (string & {});
  className?: string;
}) {
  const t = useTranslation();

  const key = POLARITY_LABEL[matchPolarity as RuleMatchPolarity];
  const label = key ? t(key) : matchPolarity;
  const inverted = matchPolarity === "MatchesWhenFalse";

  return (
    <span
      className={cn(
        "inline-flex items-center rounded border px-1.5 py-0.5 font-mono text-[11px] font-medium",
        inverted
          ? "border-slate-300 bg-card text-slate-700"
          : "border-slate-200 bg-slate-50 text-slate-500",
        className
      )}
      title={t(
        inverted ? "polarity.titleInverted" : "polarity.titleNormal"
      )}
    >
      {label}
    </span>
  );
}
