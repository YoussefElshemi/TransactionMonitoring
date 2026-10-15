import { RuleSeverity, SlaTarget } from "@/types";
import { SEVERITY_ORDER } from "@/lib/severity";

export const TARGET_MINUTES_MIN = 1;
export const TARGET_MINUTES_MAX = 525_600;

export const isValidTargetMinutes = (minutes: number): boolean =>
  Number.isInteger(minutes) &&
  minutes >= TARGET_MINUTES_MIN &&
  minutes <= TARGET_MINUTES_MAX;

export const SLA_HOUSE_DEFAULTS: Record<RuleSeverity, number> = {
  Info: 10_080,
  Low: 4_320,
  Medium: 1_440,
  High: 480,
  Critical: 120,
};

export const houseDefaultFor = (severity: string): number =>
  SLA_HOUSE_DEFAULTS[severity as RuleSeverity] ?? SLA_HOUSE_DEFAULTS.Info;

export type TargetUnit = "minutes" | "hours" | "days";

export const UNIT_MINUTES: Record<TargetUnit, number> = {
  minutes: 1,
  hours: 60,
  days: 1_440,
};

export const UNIT_LABELS: Record<TargetUnit, string> = {
  minutes: "minutes",
  hours: "hours",
  days: "days",
};

export const TARGET_UNITS: TargetUnit[] = ["days", "hours", "minutes"];

export const bestUnit = (minutes: number): TargetUnit => {
  if (minutes % UNIT_MINUTES.days === 0) return "days";
  if (minutes % UNIT_MINUTES.hours === 0) return "hours";
  return "minutes";
};

export const toUnit = (minutes: number, unit: TargetUnit): number =>
  minutes / UNIT_MINUTES[unit];

export const toMinutes = (amount: number, unit: TargetUnit): number =>
  amount * UNIT_MINUTES[unit];

export const targetLabel = (minutes: number): string => {
  const unit = bestUnit(minutes);
  const amount = toUnit(minutes, unit);
  const noun = amount === 1 ? UNIT_LABELS[unit].replace(/s$/, "") : UNIT_LABELS[unit];
  return `${amount} ${noun}`;
};

export interface SlaTargetDraft {
  severity: RuleSeverity;
  amount: string;
  unit: TargetUnit;
  useDefault: boolean;
}

export const draftMinutes = (draft: SlaTargetDraft): number => {
  const amount = draft.amount.trim();
  if (amount === "") return NaN;
  const parsed = Number(amount);
  if (!Number.isFinite(parsed)) return NaN;
  return toMinutes(parsed, draft.unit);
};

export const isDraftValid = (draft: SlaTargetDraft): boolean =>
  draft.useDefault || isValidTargetMinutes(draftMinutes(draft));

export const draftsFrom = (targets: SlaTarget[]): SlaTargetDraft[] => {
  const bySeverity = new Map(
    targets.map((target) => [String(target.severity), target.targetMinutes])
  );

  return SEVERITY_ORDER.map((severity) => {
    const minutes = bySeverity.get(severity) ?? houseDefaultFor(severity);
    const unit = bestUnit(minutes);

    return {
      severity,
      amount: String(toUnit(minutes, unit)),
      unit,
      useDefault: minutes === houseDefaultFor(severity),
    };
  });
};

export const slaTargetsBody = (
  drafts: SlaTargetDraft[]
): { targets: SlaTarget[] } => ({
  targets: drafts
    .filter((draft) => !draft.useDefault)
    .map((draft) => ({
      severity: draft.severity,
      targetMinutes: draftMinutes(draft),
    })),
});

export const isSlaDirty = (
  drafts: SlaTargetDraft[],
  original: SlaTargetDraft[]
): boolean =>
  drafts.some((draft, index) => {
    const before = original[index];
    if (!before) return true;
    if (draft.useDefault !== before.useDefault) return true;
    if (draft.useDefault) return false;
    return draftMinutes(draft) !== draftMinutes(before);
  });

export const SLA_SAVE_NOTE =
  "New targets apply to cases opened from now on. Every case already open keeps the deadline it was given.";

export const SLA_SAVED_NOTE =
  "Applies to cases opened from now on — open cases keep their deadlines.";
