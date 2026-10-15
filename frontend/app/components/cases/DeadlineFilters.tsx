"use client";

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export const BREACH_ANY = "Any";
export const BREACH_BREACHED = "Breached";
export const BREACH_WITHIN = "Within";

export const ESCALATED_ANY = "Any";
export const ESCALATED_YES = "Escalated";
export const ESCALATED_NO = "NotEscalated";

export const DUE_WITHIN_ANY = "Any";

const DUE_WITHIN_OPTIONS: {
  value: string;
  hours: number;
  labelKey: string;
}[] = [
  { value: "1", hours: 1, labelKey: "cases.fallingDueInOneHour" },
  { value: "4", hours: 4, labelKey: "cases.fallingDueInFourHours" },
  { value: "24", hours: 24, labelKey: "cases.fallingDueIn24Hours" },
  { value: "72", hours: 72, labelKey: "cases.fallingDueInThreeDays" },
];

export interface DeadlineFilterState {
  breach: string;
  escalated: string;
  dueWithin: string;
}

export const NO_DEADLINE_FILTERS: DeadlineFilterState = {
  breach: BREACH_ANY,
  escalated: ESCALATED_ANY,
  dueWithin: DUE_WITHIN_ANY,
};

export const hasDeadlineFilters = (state: DeadlineFilterState): boolean =>
  state.breach !== BREACH_ANY ||
  state.escalated !== ESCALATED_ANY ||
  state.dueWithin !== DUE_WITHIN_ANY;

export const deadlineParams = (
  state: DeadlineFilterState
): URLSearchParams => {
  const params = new URLSearchParams();

  if (state.breach === BREACH_BREACHED) params.set("Breached", "true");
  else if (state.breach === BREACH_WITHIN) params.set("Breached", "false");

  if (state.escalated === ESCALATED_YES) params.set("Escalated", "true");
  else if (state.escalated === ESCALATED_NO) params.set("Escalated", "false");

  if (state.dueWithin !== DUE_WITHIN_ANY) {
    const hours = Number(state.dueWithin);
    if (Number.isFinite(hours) && hours > 0) {
      params.set(
        "DueBefore",
        new Date(Date.now() + hours * 60 * 60 * 1000).toISOString()
      );
    }
  }

  return params;
};

export function DeadlineFilters({
  value,
  onChange,
  disabled = false,
}: {
  value: DeadlineFilterState;
  onChange: (next: DeadlineFilterState) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  const set = (patch: Partial<DeadlineFilterState>) =>
    onChange({ ...value, ...patch });

  return (
    <>
      <Select
        value={value.breach}
        onValueChange={(next) => set({ breach: next })}
        disabled={disabled}
      >
        <SelectTrigger className="w-44">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={BREACH_ANY}>{t("cases.anyDeadline")}</SelectItem>
          <SelectItem value={BREACH_BREACHED}>{t("cases.breached")}</SelectItem>
          <SelectItem value={BREACH_WITHIN}>{t("cases.insideDeadline")}</SelectItem>
        </SelectContent>
      </Select>

      <Select
        value={value.dueWithin}
        onValueChange={(next) => set({ dueWithin: next })}
        disabled={disabled}
      >
        <SelectTrigger className="w-52">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={DUE_WITHIN_ANY}>{t("cases.dueAnyTime")}</SelectItem>
          {DUE_WITHIN_OPTIONS.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {t(option.labelKey)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={value.escalated}
        onValueChange={(next) => set({ escalated: next })}
        disabled={disabled}
      >
        <SelectTrigger className="w-52">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={ESCALATED_ANY}>{t("cases.escalatedOrNot")}</SelectItem>
          <SelectItem value={ESCALATED_YES}>{t("cases.escalatedByARule")}</SelectItem>
          <SelectItem value={ESCALATED_NO}>{t("cases.noRuleHasEscalated")}</SelectItem>
        </SelectContent>
      </Select>
    </>
  );
}
