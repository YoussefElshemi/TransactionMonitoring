import type { DunningStage, DunningState } from "@/types";
import { formatMoney, toCurrencyCode } from "@/lib/currency";

export const DUNNING_STAGES: DunningStage[] = [
  "Grace",
  "FirstNotice",
  "SecondNotice",
  "FinalNotice",
  "RestrictionProposed",
];

export const stageIndex = (stage: DunningStage): number => {
  const index = DUNNING_STAGES.indexOf(stage);
  return index === -1 ? 0 : index;
};

export const DUNNING_STAGE_LABELS: Record<DunningStage, string> = {
  Grace: "reconciliation.stage.grace",
  FirstNotice: "reconciliation.stage.firstNotice",
  SecondNotice: "reconciliation.stage.secondNotice",
  FinalNotice: "reconciliation.stage.finalNotice",
  RestrictionProposed: "reconciliation.stage.restrictionProposed",
};

export const dunningStageLabelKey = (stage: DunningStage | string): string =>
  DUNNING_STAGE_LABELS[stage as DunningStage] ?? String(stage);

export const DUNNING_STAGE_HINTS: Record<DunningStage, string> = {
  Grace: "reconciliation.stageHint.grace",
  FirstNotice: "reconciliation.stageHint.firstNotice",
  SecondNotice: "reconciliation.stageHint.secondNotice",
  FinalNotice: "reconciliation.stageHint.finalNotice",
  RestrictionProposed: "reconciliation.stageHint.restrictionProposed",
};

export const dunningStageHintKey = (stage: DunningStage | string): string =>
  DUNNING_STAGE_HINTS[stage as DunningStage] ?? "";

export const needsDecision = (
  state: Pick<DunningState, "stage">
): boolean => state.stage === "RestrictionProposed";

export const NOTHING_HAS_BEEN_RESTRICTED =
  "reconciliation.nothingHasBeenRestricted";

export const LADDER_IS_NOTICES_ONLY = "reconciliation.ladderIsNoticesOnly";

export const isResolved = (
  state: Pick<DunningState, "resolvedAt">
): boolean => state.resolvedAt !== null;

export const RESOLVED_ROWS_ARE_THE_RECOVERY_FIGURE =
  "reconciliation.resolvedRowsAreTheRecoveryFigure";

export const nextEventDescriptionKey = (
  state: Pick<DunningState, "stage">
): string =>
  needsDecision(state)
    ? "reconciliation.waitingOnAPerson"
    : "reconciliation.nextCollectionAttempt";

export const hasScheduledEvent = (
  state: Pick<DunningState, "stage" | "resolvedAt">
): boolean => !needsDecision(state) && !isResolved(state);

export const hasBeenAttempted = (
  state: Pick<DunningState, "collectionAttempts">
): boolean => state.collectionAttempts > 0;

export const NEVER_ATTEMPTED = "reconciliation.neverAttempted";

export const hasBeenNoticed = (
  state: Pick<DunningState, "lastNoticedAt">
): boolean => state.lastNoticedAt !== null;

export const daysLate = (
  state: Pick<DunningState, "dueAt">,
  now: Date = new Date()
): number | null => {
  if (state.dueAt === null) return null;

  const due = new Date(state.dueAt).getTime();
  if (Number.isNaN(due)) return null;

  const days = Math.floor((now.getTime() - due) / (24 * 60 * 60 * 1000));
  return days > 0 ? days : 0;
};

export const formatOutstanding = (
  state: Pick<DunningState, "outstandingAmount" | "currency">
): string =>
  state.currency === null
    ? state.outstandingAmount.toLocaleString("en-GB", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      })
    : formatMoney(state.outstandingAmount, toCurrencyCode(state.currency));

export const CURRENCY_DID_NOT_TRAVEL = "reconciliation.currencyDidNotTravel";

export const arrearsInWorkingOrder = (
  states: readonly DunningState[]
): DunningState[] =>
  [...states].sort((left, right) => {
    const leftResolved = isResolved(left);
    const rightResolved = isResolved(right);
    if (leftResolved !== rightResolved) return leftResolved ? 1 : -1;

    const byStage = stageIndex(right.stage) - stageIndex(left.stage);
    if (byStage !== 0) return byStage;

    return (
      new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime()
    );
  });

export const decisionsWaiting = (states: readonly DunningState[]): number =>
  states.filter((state) => needsDecision(state) && !isResolved(state)).length;

export const dunningStageTone = (
  stage: DunningStage
): "inflight" | "attention" | "elevated" | "critical" => {
  switch (stage) {
    case "Grace":
      return "inflight";
    case "FirstNotice":
    case "SecondNotice":
      return "attention";
    case "FinalNotice":
      return "elevated";
    case "RestrictionProposed":
      return "critical";
    default:
      return "attention";
  }
};
