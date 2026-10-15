import type {
  AvailabilityCommitment,
  AvailabilityMeasurement,
  ServiceCreditBand,
} from "@/types";

import { formatInstant } from "@/lib/format";

export type TargetVerdict = "met" | "missed" | "none";

export const verdictOf = (met: boolean | null): TargetVerdict => {
  if (met === null) return "none";

  return met ? "met" : "missed";
};

export const VERDICT_LABELS: Record<TargetVerdict, string> = {
  met: "Met",
  missed: "Missed",
  none: "No target agreed",
};

export const verdictTone = (
  verdict: TargetVerdict
): "settled" | "critical" | "inert" => {
  switch (verdict) {
    case "met":
      return "settled";
    case "missed":
      return "critical";
    default:
      return "inert";
  }
};

export const availabilityVerdict = (
  measurement: Pick<AvailabilityMeasurement, "metAvailabilityTarget">
): TargetVerdict => verdictOf(measurement.metAvailabilityTarget);

export const latencyVerdict = (
  measurement: Pick<AvailabilityMeasurement, "metLatencyTarget">
): TargetVerdict => verdictOf(measurement.metLatencyTarget);

export const hasAgreedTarget = (
  measurement: Pick<AvailabilityMeasurement, "availabilityTarget">
): boolean => measurement.availabilityTarget !== null;

export const NO_TARGET_AGREED =
  "No availability target has been agreed for this organisation, so there is nothing to hold this month against. The figures above are still measured — there is simply no promise to compare them to.";

export const formatUptime = (percentage: number): string =>
  `${percentage.toFixed(4).replace(/\.?0+$/, "")}%`;

export const formatTarget = (target: number): string => formatUptime(target);

export const shortfall = (
  measurement: Pick<
    AvailabilityMeasurement,
    "uptimePercentage" | "availabilityTarget"
  >
): number | null => {
  if (measurement.availabilityTarget === null) return null;

  const gap = measurement.availabilityTarget - measurement.uptimePercentage;

  return gap > 0 ? gap : null;
};

export const availabilitySentence = (
  measurement: AvailabilityMeasurement
): string => {
  const measured = formatUptime(measurement.uptimePercentage);

  if (measurement.availabilityTarget === null) {
    return `${measured} of requests succeeded. No target was agreed, so this is a measurement rather than a verdict.`;
  }

  const target = formatTarget(measurement.availabilityTarget);
  const missed = shortfall(measurement);

  if (missed === null) {
    return `${measured} against a target of ${target}. The target was met.`;
  }

  return `${measured} against a target of ${target} — short by ${formatUptime(
    missed
  )}.`;
};

export const percentileSentence = (
  measurement: Pick<
    AvailabilityMeasurement,
    "percentile" | "percentileLatencyMilliseconds"
  >
): string =>
  `p${measurement.percentile} under ${measurement.percentileLatencyMilliseconds} ms`;

export const percentileLabel = (
  measurement: Pick<AvailabilityMeasurement, "percentile">
): string => `p${measurement.percentile} latency`;

export const PERCENTILE_IS_A_BOUND =
  "A bucket bound, not an exact figure. The true value is somewhere inside the bucket, so this reads worse than reality rather than better — it is the strongest claim the measurement supports.";

export const latencySentence = (
  measurement: AvailabilityMeasurement
): string => {
  const observed = percentileSentence(measurement);

  if (measurement.latencyTargetMilliseconds === null) {
    return `${observed}, with a mean of ${measurement.meanLatencyMilliseconds} ms. No latency target was agreed.`;
  }

  const verdict =
    verdictOf(measurement.metLatencyTarget) === "met" ? "met" : "missed";

  return `${observed}, against a target of ${measurement.latencyTargetMilliseconds} ms — ${verdict}.`;
};

export const failureSentence = (
  measurement: Pick<AvailabilityMeasurement, "totalRequests" | "failedRequests">
): string => {
  if (measurement.totalRequests === 0) {
    return "No requests were measured in this period, so there is nothing to work an availability figure out of.";
  }

  const share = (measurement.failedRequests / measurement.totalRequests) * 100;

  return `${measurement.failedRequests.toLocaleString(
    "en-GB"
  )} of ${measurement.totalRequests.toLocaleString("en-GB")} requests failed (${formatUptime(
    share
  )}).`;
};

export const THIN_TRAFFIC_THRESHOLD = 1000;

export const isThinlyMeasured = (
  measurement: Pick<AvailabilityMeasurement, "totalRequests">
): boolean =>
  measurement.totalRequests > 0 &&
  measurement.totalRequests < THIN_TRAFFIC_THRESHOLD;

export const thinTrafficNote = (
  measurement: Pick<AvailabilityMeasurement, "totalRequests">
): string =>
  `Only ${measurement.totalRequests.toLocaleString(
    "en-GB"
  )} requests were measured in this period. A single failure moves the percentage a long way at that volume, so read the figure as indicative rather than as a verdict.`;

export const availabilityQuery = (period: {
  fromDate: string;
  toDate: string;
}): string => {
  const start = period.fromDate.trim();
  const end = period.toDate.trim();

  if (start.length === 0 || end.length === 0) return "";
  if (!isDateOnly(start) || !isDateOnly(end)) return "";

  return `?fromDate=${encodeURIComponent(start)}&toDate=${encodeURIComponent(
    end
  )}`;
};

const DATE_ONLY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

const isDateOnly = (value: string): boolean => {
  if (!DATE_ONLY_PATTERN.test(value)) return false;
  const parsed = new Date(`${value}T00:00:00Z`);
  return (
    !Number.isNaN(parsed.getTime()) &&
    parsed.toISOString().slice(0, 10) === value
  );
};

export const periodRefusal = (period: {
  fromDate: string;
  toDate: string;
}): string | null => {
  const start = period.fromDate.trim();
  const end = period.toDate.trim();

  if (start.length === 0 && end.length === 0) return null;

  if (start.length === 0 || end.length === 0) {
    return "Give both ends or neither. Finish the selected range, or leave both empty for all recorded availability.";
  }

  if (!isDateOnly(start) || !isDateOnly(end)) {
    return "Use calendar dates in YYYY-MM-DD format.";
  }

  if (start > end) return "The end date must be at or after the start date.";

  return null;
};

export const DEFAULT_PERIOD_HINT =
  "Left empty, this measures all recorded availability. Give both dates to measure a specific span.";

export const periodLabel = (
  measurement: Pick<AvailabilityMeasurement, "periodStart" | "periodEnd">,
  locale: string,
  timeZone: string
): string => {
  const start = new Date(measurement.periodStart);
  const end = new Date(measurement.periodEnd);

  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
    return `${measurement.periodStart} to ${measurement.periodEnd}`;
  }

  return `${formatInstant(start, locale, timeZone, "date")} to ${formatInstant(
    end,
    locale,
    timeZone,
    "date"
  )}`;
};

export const TARGET_MIN = 0;
export const TARGET_MAX = 100;

export const LATENCY_TARGET_MAX_MS = 600_000;

export const PERCENTILE_MIN = 1;
export const PERCENTILE_MAX = 100;

export const isOpenTerm = (
  commitment: Pick<AvailabilityCommitment, "effectiveFrom" | "effectiveTo">,
  now: Date = new Date()
): boolean => {
  const from = Date.parse(commitment.effectiveFrom);
  const at = now.getTime();

  if (Number.isNaN(from) || from > at) return false;
  if (commitment.effectiveTo === null) return true;

  return Date.parse(commitment.effectiveTo) > at;
};

export const termState = (
  commitment: Pick<AvailabilityCommitment, "effectiveFrom" | "effectiveTo">,
  now: Date = new Date()
): "scheduled" | "current" | "superseded" => {
  const from = Date.parse(commitment.effectiveFrom);
  const at = now.getTime();

  if (!Number.isNaN(from) && from > at) return "scheduled";

  return isOpenTerm(commitment, now) ? "current" : "superseded";
};

export const CREATING_CLOSES_THE_OPEN_TERM =
  "Saving this closes whatever term is currently open, at the same instant this one starts. There is never a stretch of time a customer is measured against nothing.";

export const TARGET_HINT =
  "A percentage, not a fraction: 99.9 is three nines. Four decimal places are kept, because the difference between three nines and four is the third one.";

export const PERCENTILE_HINT =
  "Which request the latency target is about. Ninety-nine is the usual answer; the mean is not one — an average hides exactly the requests a payment integration notices.";

export const EMPTY_SCHEDULE_IS_VALID =
  "An empty schedule is a real thing to agree to: the target is measured and reported, and missing it carries no money.";

export interface CreditBandDraft {
  belowUptimePercentage: string;
  creditPercentage: string;
}

export const emptyBand = (): CreditBandDraft => ({
  belowUptimePercentage: "",
  creditPercentage: "",
});

export const bandDraftsFrom = (
  bands: readonly ServiceCreditBand[]
): CreditBandDraft[] =>
  bandsWorstFirst(bands).map((band) => ({
    belowUptimePercentage: String(band.belowUptimePercentage),
    creditPercentage: String(band.creditPercentage),
  }));

export const bandsWorstFirst = <T extends { belowUptimePercentage: number }>(
  bands: readonly T[]
): T[] =>
  [...bands].sort((a, b) => a.belowUptimePercentage - b.belowUptimePercentage);

export const bandSentence = (band: {
  belowUptimePercentage: number;
  creditPercentage: number;
}): string =>
  `Below ${formatUptime(band.belowUptimePercentage)} uptime — ${formatUptime(
    band.creditPercentage
  )} of the month's charge is credited. A month measuring exactly ${formatUptime(
    band.belowUptimePercentage
  )} is above this band and owes nothing.`;

export const bandFor = <T extends { belowUptimePercentage: number; creditPercentage: number }>(
  bands: readonly T[],
  uptimePercentage: number
): T | null => {
  const applicable = bands.filter(
    (band) => uptimePercentage < band.belowUptimePercentage
  );

  if (applicable.length === 0) return null;

  return applicable.reduce((worst, band) =>
    band.creditPercentage > worst.creditPercentage ? band : worst
  );
};

export const bandsRefusal = (drafts: readonly CreditBandDraft[]): string | null => {
  if (drafts.length === 0) return null;

  const seen = new Set<number>();

  for (const draft of drafts) {
    const below = Number(draft.belowUptimePercentage.trim());
    const credit = Number(draft.creditPercentage.trim());

    if (
      draft.belowUptimePercentage.trim().length === 0 ||
      !Number.isFinite(below)
    ) {
      return "Every band needs an uptime threshold. A band with no threshold never applies.";
    }

    if (below <= 0 || below > 100) {
      return "An uptime threshold is a percentage above zero and at most 100.";
    }

    if (draft.creditPercentage.trim().length === 0 || !Number.isFinite(credit)) {
      return "Every band needs a credit percentage.";
    }

    if (credit < 0 || credit > 100) {
      return "A credit is at least nothing and at most the whole month's charge.";
    }

    if (seen.has(below)) {
      return `Two bands both start below ${formatUptime(
        below
      )}. Only one of them could ever decide what is owed.`;
    }

    seen.add(below);
  }

  return null;
};

export const commitmentRefusal = (draft: {
  availabilityTarget: string;
  latencyTargetMilliseconds: string;
  latencyPercentile: string;
  effectiveFrom: string;
  effectiveTo: string;
  creditBands: readonly CreditBandDraft[];
}): string | null => {
  const target = Number(draft.availabilityTarget.trim());

  if (draft.availabilityTarget.trim().length === 0 || !Number.isFinite(target)) {
    return "Give an availability target.";
  }

  if (target < TARGET_MIN || target > TARGET_MAX) {
    return "A target is a percentage between 0 and 100. Remember it is a percentage and not a fraction — three nines is 99.9, not 0.999.";
  }

  const latency = Number(draft.latencyTargetMilliseconds.trim());

  if (
    draft.latencyTargetMilliseconds.trim().length === 0 ||
    !Number.isFinite(latency)
  ) {
    return "Give a latency target in milliseconds.";
  }

  if (latency < 1 || latency > LATENCY_TARGET_MAX_MS) {
    return `A latency target is between 1 ms and ${LATENCY_TARGET_MAX_MS.toLocaleString(
      "en-GB"
    )} ms. If you meant seconds, this field is in milliseconds.`;
  }

  const percentile = Number(draft.latencyPercentile.trim());

  if (
    draft.latencyPercentile.trim().length === 0 ||
    !Number.isInteger(percentile)
  ) {
    return "Give a whole-number percentile.";
  }

  if (percentile < PERCENTILE_MIN || percentile > PERCENTILE_MAX) {
    return "A percentile is between 1 and 100. Ninety-nine is the usual answer.";
  }

  if (
    draft.effectiveFrom.trim().length > 0 &&
    draft.effectiveTo.trim().length > 0
  ) {
    const from = Date.parse(draft.effectiveFrom);
    const to = Date.parse(draft.effectiveTo);

    if (!Number.isNaN(from) && !Number.isNaN(to) && to <= from) {
      return "The term ends at or before it starts.";
    }
  }

  return bandsRefusal(draft.creditBands);
};

export const commitmentBody = (draft: {
  availabilityTarget: string;
  latencyTargetMilliseconds: string;
  latencyPercentile: string;
  effectiveFrom: string;
  effectiveTo: string;
  creditBands: readonly CreditBandDraft[];
}) => ({
  availabilityTarget: Number(draft.availabilityTarget.trim()),
  latencyTargetMilliseconds: Number(draft.latencyTargetMilliseconds.trim()),
  latencyPercentile: Number(draft.latencyPercentile.trim()),
  ...(draft.effectiveFrom.trim().length > 0
    ? { effectiveFrom: new Date(draft.effectiveFrom).toISOString() }
    : {}),
  ...(draft.effectiveTo.trim().length > 0
    ? { effectiveTo: new Date(draft.effectiveTo).toISOString() }
    : {}),
  creditBands: bandsWorstFirst(
    draft.creditBands.map((band) => ({
      belowUptimePercentage: Number(band.belowUptimePercentage.trim()),
      creditPercentage: Number(band.creditPercentage.trim()),
    }))
  ),
});
