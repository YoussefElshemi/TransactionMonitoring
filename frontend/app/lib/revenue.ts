import type {
  BillableResource,
  DunningStage,
  CustomerHealth,
  CustomerMargin,
  CustomerMarginLine,
  HealthBand,
  HealthSignal,
  HealthSignalKind,
  RecoveryStatistic,
  RecurringRevenueMonth,
  RevenueConcentration,
  RevenueCurrency,
} from "@/types";
import { formatMoney, toCurrencyCode } from "@/lib/currency";
import { DUNNING_STAGES, dunningStageLabelKey, dunningStageTone } from "@/lib/dunning";

export type RevenueTone =
  | "settled"
  | "inflight"
  | "attention"
  | "elevated"
  | "critical"
  | "inert";

export type RevenueFill = "solid" | "hatch";

export const NO_ANSWER = "—";

export type NoAnswerReason =
  | "nothingBilled"
  | "noLaddersOpened"
  | "nothingResolved"
  | "noBaseline"
  | "neverUsed";

export const NO_ANSWER_REASONS: Record<NoAnswerReason, string> = {
  nothingBilled: "revenue.noAnswer.nothingBilled",
  noLaddersOpened: "revenue.noAnswer.noLaddersOpened",
  nothingResolved: "revenue.noAnswer.nothingResolved",
  noBaseline: "revenue.noAnswer.noBaseline",
  neverUsed: "revenue.noAnswer.neverUsed",
};

export interface Answer {
  known: boolean;
  text: string;
  reasonKey: string;
}

export const answered = (
  value: number | null,
  render: (value: number) => string,
  reason: NoAnswerReason
): Answer =>
  value === null
    ? { known: false, text: NO_ANSWER, reasonKey: NO_ANSWER_REASONS[reason] }
    : { known: true, text: render(value), reasonKey: "" };

export const formatRevenue = (
  amount: number,
  currency: RevenueCurrency
): string => formatMoney(amount, toCurrencyCode(currency));

export const formatSignedRevenue = (
  amount: number,
  currency: RevenueCurrency
): string => {
  const magnitude = formatRevenue(Math.abs(amount), currency);

  return amount < 0 ? `−${magnitude}` : `+${magnitude}`;
};

export interface MonthPoint {
  month: number;
  year: number;
}

export const monthOrdinal = (point: MonthPoint): number =>
  point.year * 12 + (point.month - 1);

export const isForwardRange = (from: MonthPoint, to: MonthPoint): boolean =>
  monthOrdinal(to) >= monthOrdinal(from);

export const monthsInRange = (from: MonthPoint, to: MonthPoint): number =>
  monthOrdinal(to) - monthOrdinal(from) + 1;

export const monthStart = (point: MonthPoint): Date =>
  new Date(point.year, point.month - 1, 1);

export type MovementKind =
  | "opening"
  | "new"
  | "reactivation"
  | "expansion"
  | "contraction"
  | "churn"
  | "closing";

export const MOVEMENT_ORDER: MovementKind[] = [
  "opening",
  "new",
  "reactivation",
  "expansion",
  "contraction",
  "churn",
  "closing",
];

export const MOVEMENT_LABELS: Record<MovementKind, string> = {
  opening: "revenue.movement.opening",
  new: "revenue.movement.new",
  reactivation: "revenue.movement.reactivation",
  expansion: "revenue.movement.expansion",
  contraction: "revenue.movement.contraction",
  churn: "revenue.movement.churn",
  closing: "revenue.movement.closing",
};

export const MOVEMENT_HINTS: Record<MovementKind, string> = {
  opening: "revenue.movementHint.opening",
  new: "revenue.movementHint.new",
  reactivation: "revenue.movementHint.reactivation",
  expansion: "revenue.movementHint.expansion",
  contraction: "revenue.movementHint.contraction",
  churn: "revenue.movementHint.churn",
  closing: "revenue.movementHint.closing",
};

export const MOVEMENT_APPEARANCE: Record<
  MovementKind,
  { tone: RevenueTone; fill: RevenueFill }
> = {
  opening: { tone: "inert", fill: "solid" },
  new: { tone: "settled", fill: "solid" },
  reactivation: { tone: "inflight", fill: "solid" },
  expansion: { tone: "settled", fill: "hatch" },
  contraction: { tone: "elevated", fill: "solid" },
  churn: { tone: "critical", fill: "solid" },
  closing: { tone: "inert", fill: "solid" },
};

export interface MovementStep {
  kind: MovementKind;
  labelKey: string;
  hintKey: string;
  /** Signed: contraction and churn arrive as magnitudes and are negated here. */
  amount: number;
  from: number;
  to: number;
  isTotal: boolean;
  direction: "up" | "down" | "total";
  tone: RevenueTone;
  fill: RevenueFill;
  customers: number | null;
}

export const movementSteps = (month: RecurringRevenueMonth): MovementStep[] => {
  const step = (
    kind: MovementKind,
    amount: number,
    from: number,
    customers: number | null
  ): MovementStep => ({
    kind,
    labelKey: MOVEMENT_LABELS[kind],
    hintKey: MOVEMENT_HINTS[kind],
    amount,
    from,
    to: from + amount,
    isTotal: false,
    direction: amount < 0 ? "down" : "up",
    ...MOVEMENT_APPEARANCE[kind],
    customers,
  });

  const total = (kind: MovementKind, value: number, customers: number): MovementStep => ({
    kind,
    labelKey: MOVEMENT_LABELS[kind],
    hintKey: MOVEMENT_HINTS[kind],
    amount: value,
    from: 0,
    to: value,
    isTotal: true,
    direction: "total",
    ...MOVEMENT_APPEARANCE[kind],
    customers,
  });

  const steps: MovementStep[] = [
    total("opening", month.openingRevenue, month.openingCustomers),
  ];

  let running = month.openingRevenue;
  const float = (
    kind: MovementKind,
    amount: number,
    customers: number | null
  ) => {
    const next = step(kind, amount, running, customers);
    running = next.to;
    steps.push(next);
  };

  float("new", month.newRevenue, month.newCustomers);
  float("reactivation", month.reactivationRevenue, month.reactivatedCustomers);
  float("expansion", month.expansionRevenue, null);
  float("contraction", -month.contractionRevenue, null);
  float("churn", -month.churnedRevenue, month.churnedCustomers);

  steps.push(total("closing", month.closingRevenue, month.closingCustomers));

  return steps;
};

export const impliedClosing = (month: RecurringRevenueMonth): number =>
  month.openingRevenue +
  month.newRevenue +
  month.reactivationRevenue +
  month.expansionRevenue -
  month.contractionRevenue -
  month.churnedRevenue;

export const movementResidual = (month: RecurringRevenueMonth): number =>
  month.closingRevenue - impliedClosing(month);

export const movementReconciles = (
  month: RecurringRevenueMonth,
  tolerance = 0.005
): boolean => Math.abs(movementResidual(month)) <= tolerance;

export const NET_GROWTH_IS_NOT_THE_HEADLINE =
  "revenue.netGrowthIsNotTheHeadline";

export const MOVEMENT_DOES_NOT_RECONCILE = "revenue.movementDoesNotReconcile";

export const movementExtent = (
  steps: readonly MovementStep[]
): { min: number; max: number } => {
  const values = steps.flatMap((s) => [s.from, s.to]);

  return {
    min: Math.min(0, ...values),
    max: Math.max(0, ...values),
  };
};

export const involuntaryShareOfChurn = (
  month: Pick<RecurringRevenueMonth, "churnedRevenue" | "involuntaryChurnedRevenue">
): number | null =>
  month.churnedRevenue === 0
    ? null
    : month.involuntaryChurnedRevenue / month.churnedRevenue;

export const INVOLUNTARY_IS_INSIDE_CHURN = "revenue.involuntaryIsInsideChurn";

export const INVOLUNTARY_APPEARANCE: { tone: RevenueTone; fill: RevenueFill } = {
  tone: "critical",
  fill: "hatch",
};

export const DEFAULT_TOP_COUNT = 20;
export const MAX_TOP_COUNT = 500;

export const clampTopCount = (value: number): number =>
  Math.min(MAX_TOP_COUNT, Math.max(1, Math.trunc(value) || DEFAULT_TOP_COUNT));

export const equivalentCustomers = (
  concentrationIndex: number | null
): number | null =>
  concentrationIndex === null || concentrationIndex <= 0
    ? null
    : 1 / concentrationIndex;

export type ConcentrationReading = "spread" | "concentrated" | "dependent";

export const concentrationReading = (
  concentrationIndex: number | null
): ConcentrationReading | null => {
  if (concentrationIndex === null) return null;
  if (concentrationIndex >= 0.25) return "dependent";
  if (concentrationIndex >= 0.15) return "concentrated";

  return "spread";
};

export const CONCENTRATION_READING_LABELS: Record<ConcentrationReading, string> = {
  spread: "revenue.concentration.spread",
  concentrated: "revenue.concentration.concentrated",
  dependent: "revenue.concentration.dependent",
};

export const concentrationTone = (
  reading: ConcentrationReading
): RevenueTone => {
  switch (reading) {
    case "spread":
      return "settled";
    case "concentrated":
      return "inflight";
    default:
      return "attention";
  }
};

export const CUSTOMER_COUNT_IS_NOT_A_TENANT_COUNT =
  "revenue.customerCountIsNotATenantCount";

export const MONTH_BILLED_NOTHING = "revenue.monthBilledNothing";

export const monthHasRevenue = (
  concentration: Pick<RevenueConcentration, "totalRevenue" | "largestCustomerShare">
): boolean =>
  concentration.largestCustomerShare !== null && concentration.totalRevenue > 0;

export const marginIsOverstated = (
  margin: Pick<CustomerMargin, "costsComplete">
): boolean => !margin.costsComplete;

export const uncostedLines = (
  margin: Pick<CustomerMargin, "lines">
): CustomerMarginLine[] =>
  margin.lines.filter((line) => line.unitCost === null);

export const COSTS_INCOMPLETE = "revenue.costsIncomplete";

export const COSTS_INCOMPLETE_EXPLAINED = "revenue.costsIncompleteExplained";

export const SOME_MARGINS_ARE_OVERSTATED = "revenue.someMarginsAreOverstated";

export const marginTone = (margin: CustomerMargin): RevenueTone => {
  if (marginIsOverstated(margin)) return "attention";
  if (margin.marginRate === null) return "inert";
  if (margin.marginRate < 0) return "critical";
  if (margin.marginRate < 0.2) return "elevated";

  return "settled";
};

export const marginRateAnswer = (
  margin: Pick<CustomerMargin, "marginRate">,
  render: (fraction: number) => string
): Answer => answered(margin.marginRate, render, "nothingBilled");

export const RESOURCE_LABELS: Record<BillableResource, string> = {
  Unclassified: "revenue.resource.unclassified",
  RuleEvaluation: "revenue.resource.ruleEvaluation",
  RuleSetEvaluation: "revenue.resource.ruleSetEvaluation",
  RuleTest: "revenue.resource.ruleTest",
  Backtest: "revenue.resource.backtest",
  SandboxWrite: "revenue.resource.sandboxWrite",
};

export const resourceLabelKey = (resource: BillableResource | string): string =>
  RESOURCE_LABELS[resource as BillableResource] ?? String(resource);

export const RECOVERY_IS_A_COHORT = "revenue.recoveryIsACohort";

export const LADDER_RUNS_FOR_DAYS = 60;

export const cohortIsStillMaturing = (
  statistic: Pick<RecoveryStatistic, "periodEnd">,
  now: Date = new Date()
): boolean => {
  const end = new Date(statistic.periodEnd).getTime();
  if (Number.isNaN(end)) return false;

  const daysSinceEnd = (now.getTime() - end) / (24 * 60 * 60 * 1000);

  return daysSinceEnd < LADDER_RUNS_FOR_DAYS;
};

export const COHORT_IS_STILL_MATURING = "revenue.cohortIsStillMaturing";

export interface RecoveryStageRow {
  stage: DunningStage;
  labelKey: string;
  tone: RevenueTone;
  resolved: number;
  share: number | null;
}

export const recoveryStageRows = (
  statistic: Pick<RecoveryStatistic, "resolvedByStage">
): RecoveryStageRow[] => {
  const resolvedAt = new Map(
    statistic.resolvedByStage.map((row) => [row.stage, row.resolved])
  );
  const total = statistic.resolvedByStage.reduce(
    (sum, row) => sum + row.resolved,
    0
  );

  return DUNNING_STAGES.map((stage) => ({
    stage,
    labelKey: dunningStageLabelKey(stage),
    tone: dunningStageTone(stage),
    resolved: resolvedAt.get(stage) ?? 0,
    share: total === 0 ? null : (resolvedAt.get(stage) ?? 0) / total,
  }));
};

export const RESTRICTIONS_PROPOSED_EXPLAINED =
  "revenue.restrictionsProposedExplained";

export const HEALTH_BANDS: HealthBand[] = ["AtRisk", "Watch", "Healthy"];

export const HEALTH_BAND_LABELS: Record<HealthBand, string> = {
  Healthy: "revenue.band.healthy",
  Watch: "revenue.band.watch",
  AtRisk: "revenue.band.atRisk",
};

export const HEALTH_BAND_HINTS: Record<HealthBand, string> = {
  Healthy: "revenue.bandHint.healthy",
  Watch: "revenue.bandHint.watch",
  AtRisk: "revenue.bandHint.atRisk",
};

export const healthBandTone = (band: HealthBand): RevenueTone => {
  switch (band) {
    case "Healthy":
      return "settled";
    case "Watch":
      return "attention";
    default:
      return "elevated";
  }
};

export const SCORE_IS_A_SORT_NOT_A_VERDICT = "revenue.scoreIsASortNotAVerdict";

export const SIGNAL_LABELS: Record<HealthSignalKind, string> = {
  UsageDecline: "revenue.signal.usageDecline",
  ErrorRateRise: "revenue.signal.errorRateRise",
  Silence: "revenue.signal.silence",
  Arrears: "revenue.signal.arrears",
  NoBaseline: "revenue.signal.noBaseline",
};

export const isAbsenceOfEvidence = (kind: HealthSignalKind): boolean =>
  kind === "NoBaseline";

export const signalTone = (signal: HealthSignal): RevenueTone => {
  if (isAbsenceOfEvidence(signal.kind)) return "inert";
  if (signal.penalty >= 25) return "elevated";
  if (signal.penalty >= 10) return "attention";

  return "inflight";
};

export const signalsInReadingOrder = (
  signals: readonly HealthSignal[]
): HealthSignal[] => {
  const declared = Object.keys(SIGNAL_LABELS) as HealthSignalKind[];

  return [...signals].sort((left, right) => {
    const leftAbsence = isAbsenceOfEvidence(left.kind);
    const rightAbsence = isAbsenceOfEvidence(right.kind);
    if (leftAbsence !== rightAbsence) return leftAbsence ? 1 : -1;

    const byPenalty = right.penalty - left.penalty;
    if (byPenalty !== 0) return byPenalty;

    return declared.indexOf(left.kind) - declared.indexOf(right.kind);
  });
};

export interface SignalNumbers {
  percent: (fraction: number) => string;
  count: (value: number) => string;
}

export const signalSentence = (
  health: Pick<
    CustomerHealth,
    "usageTrend" | "recentErrorRate" | "previousErrorRate" | "daysSinceLastUsage"
  >,
  signal: HealthSignal,
  numbers: SignalNumbers
): { key: string; args: (string | number)[] } => {
  switch (signal.kind) {
    case "UsageDecline":
      return health.usageTrend === null
        ? { key: "revenue.signalSentence.usageDeclineUnmeasured", args: [] }
        : {
            key: "revenue.signalSentence.usageDecline",
            args: [numbers.percent(Math.abs(health.usageTrend))],
          };

    case "ErrorRateRise":
      return {
        key: "revenue.signalSentence.errorRateRise",
        args: [
          numbers.percent(health.previousErrorRate),
          numbers.percent(health.recentErrorRate),
        ],
      };

    case "Silence":
      return health.daysSinceLastUsage === null
        ? { key: "revenue.signalSentence.silenceNeverUsed", args: [] }
        : {
            key: "revenue.signalSentence.silence",
            args: [numbers.count(health.daysSinceLastUsage)],
          };

    case "Arrears":
      return { key: "revenue.signalSentence.arrears", args: [] };

    default:
      return { key: "revenue.signalSentence.noBaseline", args: [] };
  }
};

export const PLATFORM_WROTE_THIS = "revenue.platformWroteThis";

export const arrearsChip = (
  health: Pick<CustomerHealth, "arrearsStage">
): { labelKey: string; tone: RevenueTone } | null =>
  health.arrearsStage === null
    ? null
    : {
        labelKey: dunningStageLabelKey(health.arrearsStage),
        tone: dunningStageTone(health.arrearsStage),
      };

export const DEFAULT_WINDOW_DAYS = 30;
export const MAX_WINDOW_DAYS = 365;

export const clampWindowDays = (value: number): number =>
  Math.min(MAX_WINDOW_DAYS, Math.max(1, Math.trunc(value) || DEFAULT_WINDOW_DAYS));

export const usageTrendAnswer = (
  health: Pick<CustomerHealth, "usageTrend">,
  render: (fraction: number) => string
): Answer => answered(health.usageTrend, render, "noBaseline");

export const recognitionQuery = (
  from: MonthPoint,
  to: MonthPoint
): URLSearchParams =>
  new URLSearchParams({
    fromMonth: String(from.month),
    fromYear: String(from.year),
    toMonth: String(to.month),
    toYear: String(to.year),
  });

export const recognitionFileName = (
  from: MonthPoint,
  to: MonthPoint
): string => {
  const stamp = (point: MonthPoint) =>
    `${point.year}-${String(point.month).padStart(2, "0")}`;

  return `revenue-recognition-${stamp(from)}-to-${stamp(to)}.csv`;
};

export const RECOGNITION_EXPLAINED = "revenue.recognitionExplained";

export const uncostedResources = (
  rates: readonly { billableResource: BillableResource }[],
  resources: readonly BillableResource[]
): BillableResource[] => {
  const costed = new Set(rates.map((rate) => rate.billableResource));

  return resources.filter((resource) => !costed.has(resource));
};

export const isValidUnitCost = (value: string): boolean => {
  const trimmed = value.trim();
  if (trimmed === "") return false;

  const parsed = Number(trimmed);

  return Number.isFinite(parsed) && parsed >= 0;
};

export const RATES_ARE_APPENDED = "revenue.ratesAreAppended";

export const RESOURCE_HAS_NO_RATE = "revenue.resourceHasNoRate";
