import {
  SchemaField,
  SchemaFieldType,
  VelocityConditionDto,
  VelocityFunction,
  VelocityMetric,
  VelocityProfileUsage,
  VelocityThreshold,
  VelocityWindowDto,
  VelocityWindowKind,
  VelocityWindowUnit,
} from "@/types";
import { parsePath } from "@/lib/payloadPath";
import {
  SchemaPath,
  resolveSchemaPath,
  schemaPaths,
} from "@/lib/transactionSchema";

export const MAX_METRICS = 25;
export const MAX_THRESHOLDS = 25;
export const MAX_NAME_LENGTH = 64;

export const ROLLING_MAX_LENGTH: Record<VelocityWindowUnit, number> = {
  Minute: 527040,
  Hour: 8784,
  Day: 366,
  Week: 53,
  Month: 12,
};

// Sum/Min/Max/Average answer null over an empty window, and null never matches.
export const ANSWERS_ZERO_ON_EMPTY = new Set<string>([
  "Count",
  "DistinctCount",
  "StructuringCount",
]);

export const VELOCITY_FUNCTION_LABEL: Record<VelocityFunction, string> = {
  Count: "Count",
  DistinctCount: "Distinct count",
  Sum: "Sum",
  Min: "Minimum",
  Max: "Maximum",
  Average: "Average",
  StructuringCount: "Structuring count",
};

export const VELOCITY_FUNCTION_HINT: Record<VelocityFunction, string> = {
  Count:
    "How many payments this party stood in over the window. Answers 0 over an empty window, so a rule can be written about a party who has done nothing.",
  DistinctCount:
    "How many different values of the metric the window holds — distinct beneficiaries in an hour, distinct countries in a day. Answers 0 over an empty window.",
  Sum: "The metric added up over the window. Answers nothing over an empty window, and a leaf comparing nothing never matches — a sum of no payments is not zero pounds.",
  Min: "The smallest value of the metric in the window. Answers nothing over an empty window, and a leaf comparing nothing never matches.",
  Max: "The largest value of the metric in the window. Answers nothing over an empty window, and a leaf comparing nothing never matches.",
  Average:
    "The mean value of the metric over the window. Answers nothing over an empty window, and a leaf comparing nothing never matches.",
  StructuringCount:
    "How many payments in the window sat just under a threshold the profile declares — the shape of somebody splitting one reportable payment into several. Answers 0 over an empty window.",
};

export const METRIC_TYPES: SchemaFieldType[] = ["String", "Number", "DateTime"];

export const isMetricType = (type: SchemaFieldType): boolean =>
  METRIC_TYPES.includes(type);

export const WINDOW_KIND_HINT: Record<VelocityWindowKind, string> = {
  Rolling:
    "Counts back from the moment the payment is judged. Needs a length, and has no boundary for a time zone to place.",
  Calendar:
    "Covers the period the payment falls in — today, this week, this month. Takes no length: it is a period, not a span. Weeks start on Monday.",
};

export interface DraftMetric {
  id: string;
  name: string;
  path: string;
}

export interface DraftThreshold {
  id: string;
  name: string;
  value: string;
}

let nextDraftId = 0;
const draftId = (prefix: string): string => `${prefix}-${nextDraftId++}`;

export const newDraftMetric = (): DraftMetric => ({
  id: draftId("metric"),
  name: "",
  path: "",
});

export const newDraftThreshold = (): DraftThreshold => ({
  id: draftId("threshold"),
  name: "",
  value: "",
});

export const toMetricDrafts = (metrics: VelocityMetric[]): DraftMetric[] =>
  metrics.map((metric) => ({
    id: draftId("metric"),
    name: metric.name,
    path: metric.path,
  }));

export const toThresholdDrafts = (
  thresholds: VelocityThreshold[]
): DraftThreshold[] =>
  thresholds.map((threshold) => ({
    id: draftId("threshold"),
    name: threshold.name,
    value: String(threshold.value),
  }));

export const toRequestMetrics = (drafts: DraftMetric[]): VelocityMetric[] =>
  drafts
    .filter((draft) => draft.name.trim() || draft.path.trim())
    .map((draft) => ({ name: draft.name.trim(), path: draft.path.trim() }));

export const toRequestThresholds = (
  drafts: DraftThreshold[]
): VelocityThreshold[] =>
  drafts
    .filter((draft) => draft.name.trim() || draft.value.trim())
    .map((draft) => ({
      name: draft.name.trim(),
      value: Number(draft.value.trim()),
    }));

export const sameProfile = (
  a: { metrics: VelocityMetric[]; thresholds: VelocityThreshold[] },
  b: { metrics: VelocityMetric[]; thresholds: VelocityThreshold[] }
): boolean => JSON.stringify(a) === JSON.stringify(b);

export const metricNames = (
  metrics: VelocityMetric[] | null | undefined
): string[] => (metrics ?? []).map((metric) => metric.name);

export const thresholdNames = (
  thresholds: VelocityThreshold[] | null | undefined
): string[] => (thresholds ?? []).map((threshold) => threshold.name);

export const declares = (
  names: string[] | null | undefined,
  name: string | null | undefined
): boolean => {
  const wanted = (name ?? "").trim().toLowerCase();
  if (!wanted) return false;
  return (names ?? []).some((candidate) => candidate.toLowerCase() === wanted);
};

export const thresholdValue = (
  thresholds: VelocityThreshold[] | null | undefined,
  name: string | null | undefined
): number | null => {
  const wanted = (name ?? "").trim().toLowerCase();
  if (!wanted) return null;
  const found = (thresholds ?? []).find(
    (threshold) => threshold.name.toLowerCase() === wanted
  );
  return found ? found.value : null;
};

export const metricPaths = (
  fields: SchemaField[] | null | undefined
): SchemaPath[] =>
  fields
    ? schemaPaths(fields).filter(
        (path) =>
          isMetricType(path.type) &&
          !path.multiple &&
          !path.path.includes("[*]")
      )
    : [];

export const staleMetrics = (
  metrics: VelocityMetric[] | null | undefined,
  fields: SchemaField[] | null | undefined
): VelocityMetric[] => {
  if (!fields || !metrics) return [];
  return metrics.filter((metric) => {
    const path = metric.path.trim();
    if (!path) return false;
    const resolution = resolveSchemaPath(fields, path);
    return resolution === null || !isMetricType(resolution.type);
  });
};

export type MetricField = "name" | "path";
export type ThresholdField = "name" | "value";

export interface DraftValidation {
  metricErrors: Record<string, Partial<Record<MetricField, string>>>;
  thresholdErrors: Record<string, Partial<Record<ThresholdField, string>>>;
  formError: string | null;
}

const nameProblem = (name: string, what: string): string | null => {
  if (!name) return `Give the ${what} a name — it is what a rule addresses it by.`;
  if (name.length > MAX_NAME_LENGTH) {
    return `A ${what} name is at most ${MAX_NAME_LENGTH} characters.`;
  }
  return null;
};

export const validateDrafts = (
  metrics: DraftMetric[],
  thresholds: DraftThreshold[],
  fields: SchemaField[] | null
): DraftValidation => {
  const metricErrors: DraftValidation["metricErrors"] = {};
  const thresholdErrors: DraftValidation["thresholdErrors"] = {};
  let formError: string | null = null;

  const filledMetrics = metrics.filter(
    (metric) => metric.name.trim() || metric.path.trim()
  );

  if (filledMetrics.length === 0) {
    formError =
      "A velocity profile must declare at least one metric. To stop observing altogether, withdraw the profile instead — that keeps the observations already taken.";
  } else if (filledMetrics.length > MAX_METRICS) {
    formError = `A profile declares at most ${MAX_METRICS} metrics.`;
  } else if (thresholds.length > MAX_THRESHOLDS) {
    formError = `A profile declares at most ${MAX_THRESHOLDS} thresholds.`;
  }

  const markMetric = (id: string, field: MetricField, message: string) => {
    metricErrors[id] ??= {};
    metricErrors[id][field] ??= message;
  };

  const markThreshold = (
    id: string,
    field: ThresholdField,
    message: string
  ) => {
    thresholdErrors[id] ??= {};
    thresholdErrors[id][field] ??= message;
  };

  const seenMetrics = new Set<string>();

  for (const metric of metrics) {
    const name = metric.name.trim();
    const path = metric.path.trim();

    if (!name && !path) continue;

    const problem = nameProblem(name, "metric");
    if (problem) {
      markMetric(metric.id, "name", problem);
    } else if (seenMetrics.has(name.toLowerCase())) {
      markMetric(
        metric.id,
        "name",
        `'${name}' is already declared. Names are matched case-insensitively, so a rule naming it would have two figures to choose between.`
      );
    }
    if (name) seenMetrics.add(name.toLowerCase());

    if (!path) {
      markMetric(
        metric.id,
        "path",
        "Say where in the payload this figure is read from."
      );
      continue;
    }

    if (path.includes("*")) {
      markMetric(
        metric.id,
        "path",
        "A metric is one figure per payment per party, so its path cannot carry a wildcard — the store has nowhere to put a second. Point at the one value this metric means."
      );
      continue;
    }

    if (!parsePath(path)) {
      markMetric(
        metric.id,
        "path",
        "This is not a path the engine can read. Members are addressed as amount, or as account.balance."
      );
      continue;
    }

    if (fields) {
      const resolution = resolveSchemaPath(fields, path);
      if (!resolution) {
        markMetric(
          metric.id,
          "path",
          "The transaction schema does not declare this path, and the API refuses a metric that reads nothing."
        );
      } else if (!isMetricType(resolution.type)) {
        markMetric(
          metric.id,
          "path",
          resolution.type === "Boolean"
            ? "A true/false field is neither a figure to total nor an identity to count — a distinct count over it answers two for every organisation. Point at a value with something to measure."
            : `The schema declares this as ${resolution.type}, which carries no single value a metric could keep. Declare a field inside it instead.`
        );
      }
    }
  }

  const seenThresholds = new Set<string>();

  for (const threshold of thresholds) {
    const name = threshold.name.trim();
    const raw = threshold.value.trim();

    if (!name && !raw) continue;

    const problem = nameProblem(name, "threshold");
    if (problem) {
      markThreshold(threshold.id, "name", problem);
    } else if (seenThresholds.has(name.toLowerCase())) {
      markThreshold(
        threshold.id,
        "name",
        `'${name}' is already declared.`
      );
    }
    if (name) seenThresholds.add(name.toLowerCase());

    if (!raw) {
      markThreshold(threshold.id, "value", "Give the threshold a figure.");
      continue;
    }

    const value = Number(raw);
    if (!Number.isFinite(value)) {
      markThreshold(threshold.id, "value", "This is not a number.");
    } else if (value <= 0) {
      markThreshold(
        threshold.id,
        "value",
        "A threshold is the figure a duty starts at, so it has to be above zero."
      );
    }
  }

  return { metricErrors, thresholdErrors, formError };
};

export const hasErrors = (validation: DraftValidation): boolean =>
  validation.formError !== null ||
  Object.values(validation.metricErrors).some(
    (row) => Object.keys(row).length > 0
  ) ||
  Object.values(validation.thresholdErrors).some(
    (row) => Object.keys(row).length > 0
  );

export interface Breakage {
  usage: VelocityProfileUsage;
  kind: "metric" | "threshold";
  name: string;
}

export const breakagesOf = (
  proposedMetrics: VelocityMetric[],
  proposedThresholds: VelocityThreshold[],
  usages: VelocityProfileUsage[]
): Breakage[] => {
  const metrics = metricNames(proposedMetrics);
  const thresholds = thresholdNames(proposedThresholds);
  const breakages: Breakage[] = [];

  for (const usage of usages) {
    for (const metric of usage.metrics ?? []) {
      if (!declares(metrics, metric)) {
        breakages.push({ usage, kind: "metric", name: metric });
      }
    }
    for (const threshold of usage.thresholds ?? []) {
      if (!declares(thresholds, threshold)) {
        breakages.push({ usage, kind: "threshold", name: threshold });
      }
    }
  }

  return breakages;
};

export const describeBreakage = (breakage: Breakage): string =>
  breakage.kind === "metric"
    ? `aggregates the metric '${breakage.name}', which this save would stop declaring`
    : `reads the threshold '${breakage.name}', which this save would stop declaring`;

export const introducedMetrics = (
  proposed: VelocityMetric[],
  current: VelocityMetric[] | null
): string[] => {
  const existing = metricNames(current);
  return proposed
    .map((metric) => metric.name)
    .filter((name) => !declares(existing, name));
};

const lowerFirst = (value: string): string =>
  value ? value.charAt(0).toLowerCase() + value.slice(1) : value;

export const describeWindow = (
  window: VelocityWindowDto | null | undefined
): string => {
  if (!window) return "no window";

  const unit = String(window.unit ?? "").toLowerCase() || "period";

  if (window.kind === "Calendar") {
    const zone = window.timeZone?.trim();
    return zone ? `this ${unit} in ${zone}` : `this ${unit}`;
  }

  const length = window.length ?? null;
  if (length === null) return `last ? ${unit}s`;
  return `last ${length} ${unit}${length === 1 ? "" : "s"}`;
};

export const velocityLeafName = (
  velocity: VelocityConditionDto | null | undefined
): string => {
  if (!velocity) return "";

  const parts = [
    `velocity.${lowerFirst(String(velocity.role ?? ""))}.${velocity.metric ?? ""}`,
    describeWindow(velocity.window),
    velocity.includeCurrent === true
      ? "including current"
      : velocity.includeCurrent === false
        ? "excluding current"
        : "current unset",
  ];

  if (velocity.function === "StructuringCount") {
    const band =
      typeof velocity.band === "number" ? formatBand(velocity.band) : "?";
    parts.push(`just under ${velocity.threshold ?? "?"} by ${band}`);
  }

  return `${lowerFirst(String(velocity.function ?? "aggregate"))}(${parts.join(", ")})`;
};

// includeCurrent is left undefined on purpose: the API requires an explicit choice.
export const newVelocityCondition = (role: string): VelocityConditionDto => ({
  role,
  metric: "",
  function: "Sum",
  window: { kind: "Rolling", unit: "Day", length: 1 },
});

export const withVelocityFunction = (
  velocity: VelocityConditionDto,
  next: VelocityFunction
): VelocityConditionDto => {
  if (next === "StructuringCount") {
    return { ...velocity, function: next };
  }
  const { threshold: _t, band: _b, ...rest } = velocity; // eslint-disable-line @typescript-eslint/no-unused-vars
  return { ...rest, function: next };
};

export const withWindowKind = (
  window: VelocityWindowDto,
  kind: VelocityWindowKind
): VelocityWindowDto =>
  kind === "Calendar"
    ? { kind, unit: window.unit, ...(window.timeZone ? { timeZone: window.timeZone } : {}) }
    : { kind, unit: window.unit, length: window.length ?? 1 };

export const formatBand = (band: number): string => {
  const percent = band * 100;
  const rounded = Math.round(percent * 1000) / 1000;
  return `${rounded}%`;
};

export const structuringRange = (
  threshold: number,
  band: number
): { from: number; to: number } => ({
  from: threshold - threshold * band,
  to: threshold,
});

export const timeZones = (): string[] => {
  try {
    const supported = (
      Intl as unknown as { supportedValuesOf?: (key: string) => string[] }
    ).supportedValuesOf;
    return supported ? supported("timeZone") : [];
  } catch {
    return [];
  }
};

export const isKnownTimeZone = (id: string): boolean => {
  const trimmed = id.trim();
  if (!trimmed) return false;
  try {
    new Intl.DateTimeFormat("en-GB", { timeZone: trimmed });
    return true;
  } catch {
    return false;
  }
};

export const validateVelocity = (
  velocity: VelocityConditionDto | null | undefined,
  declared: {
    metrics: string[] | null;
    thresholds: string[] | null;
  }
): string[] => {
  if (!velocity) return [];

  const problems: string[] = [];

  if (!String(velocity.role ?? "").trim()) {
    problems.push("Say whose window this is — the remitter's or the beneficiary's.");
  }

  const metric = String(velocity.metric ?? "").trim();
  if (!metric) {
    problems.push("Choose the metric to aggregate.");
  } else if (declared.metrics && !declares(declared.metrics, metric)) {
    problems.push(
      `The velocity profile does not declare a metric called '${metric}', so there is nothing for this to aggregate.`
    );
  }

  const fn = String(velocity.function ?? "");
  if (!fn) problems.push("Choose what to do with the observations in the window.");

  const window = velocity.window;
  if (!window) {
    problems.push("Say what window this reads over.");
  } else {
    const unit = String(window.unit ?? "");
    if (!unit) problems.push("Say what the window is counted in.");

    if (window.kind === "Rolling") {
      const length = window.length;
      const max = ROLLING_MAX_LENGTH[unit as VelocityWindowUnit];
      if (length === null || length === undefined) {
        problems.push(
          "A rolling window counts back from the moment of judgement, so it needs a length."
        );
      } else if (!Number.isInteger(length) || length < 1) {
        problems.push("A window length is a whole number of one or more.");
      } else if (max !== undefined && length > max) {
        problems.push(
          `A window cannot reach back further than about a year — at most ${max} ${unit.toLowerCase()}${max === 1 ? "" : "s"}.`
        );
      }
      if (window.timeZone?.trim()) {
        problems.push(
          "A rolling window has no boundary for a time zone to place, and the API rejects one. Use a calendar window if the boundary matters."
        );
      }
    } else if (window.kind === "Calendar") {
      if (window.length !== null && window.length !== undefined) {
        problems.push(
          "A calendar window covers the period the payment falls in, so it takes no length. 'The last 3 days' is a rolling window."
        );
      }
      const zone = window.timeZone?.trim();
      if (zone && !isKnownTimeZone(zone)) {
        problems.push(
          `'${zone}' is not a time zone this browser knows. Zones are IANA ids, as Europe/London.`
        );
      }
    } else if (window.kind) {
      problems.push(`'${window.kind}' is not a window kind the engine knows.`);
    } else {
      problems.push("Say whether the window rolls back or covers a calendar period.");
    }
  }

  if (typeof velocity.includeCurrent !== "boolean") {
    problems.push(
      "Say whether this payment counts towards its own window. There is no default: it is the difference between stopping the payment that breaches a limit and stopping the one after it."
    );
  }

  if (fn === "StructuringCount") {
    const threshold = String(velocity.threshold ?? "").trim();
    if (!threshold) {
      problems.push(
        "A structuring count needs a threshold — the figure the payments are sitting just under."
      );
    } else if (declared.thresholds && !declares(declared.thresholds, threshold)) {
      problems.push(
        `The velocity profile does not declare a threshold called '${threshold}'.`
      );
    }

    const band = velocity.band;
    if (band === null || band === undefined) {
      problems.push(
        "A structuring count needs a band — how close to the threshold counts as just under it."
      );
    } else if (!Number.isFinite(band) || band <= 0 || band > 1) {
      problems.push(
        "The band is a fraction of the threshold, above 0 and at most 1 — a whole 100% reaches from zero up to the threshold."
      );
    }
  } else {
    if (velocity.threshold !== null && velocity.threshold !== undefined) {
      problems.push(
        "Only a structuring count reads a threshold, and the API refuses one on any other function."
      );
    }
    if (velocity.band !== null && velocity.band !== undefined) {
      problems.push(
        "Only a structuring count takes a band, and the API refuses one on any other function."
      );
    }
  }

  return [...new Set(problems)];
};

export const VELOCITY_PROFILE_IN_USE = "VelocityProfileInUseException";

export const readVelocityUsages = (
  body: unknown
): VelocityProfileUsage[] | null => {
  const problem = body as { type?: string; usages?: unknown } | null;
  if (!problem || problem.type !== VELOCITY_PROFILE_IN_USE) return null;
  return Array.isArray(problem.usages)
    ? (problem.usages as VelocityProfileUsage[])
    : [];
};

export const describeUsage = (usage: VelocityProfileUsage): string =>
  [...(usage.metrics ?? []), ...(usage.thresholds ?? [])].join(", ");

const parseList = <T>(
  value: unknown,
  read: (record: Record<string, unknown>) => T
): T[] | null => {
  let raw: unknown = value;

  if (typeof value === "string") {
    try {
      raw = JSON.parse(value);
    } catch {
      return null;
    }
  }

  if (!Array.isArray(raw)) return null;

  const entries: T[] = [];
  for (const candidate of raw) {
    if (candidate === null || typeof candidate !== "object") return null;
    entries.push(read(candidate as Record<string, unknown>));
  }
  return entries;
};

export const parseVelocityMetrics = (
  value: unknown
): VelocityMetric[] | null =>
  parseList(value, (record) => ({
    name: String(record.Name ?? record.name ?? ""),
    path: String(record.Path ?? record.path ?? ""),
  }));

export const parseVelocityThresholds = (
  value: unknown
): VelocityThreshold[] | null =>
  parseList(value, (record) => ({
    name: String(record.Name ?? record.name ?? ""),
    value: Number(record.Value ?? record.value ?? 0),
  }));

export type ProfileChange = "added" | "removed" | "changed" | "unchanged";

export interface ProfileDiffRow {
  name: string;
  change: ProfileChange;
  before: string | null;
  after: string | null;
}

export const diffNamed = (
  before: { name: string; describe: string }[] | null,
  after: { name: string; describe: string }[] | null
): ProfileDiffRow[] => {
  const beforeByName = new Map((before ?? []).map((e) => [e.name, e.describe]));
  const afterByName = new Map((after ?? []).map((e) => [e.name, e.describe]));

  const names = [
    ...afterByName.keys(),
    ...[...beforeByName.keys()].filter((name) => !afterByName.has(name)),
  ];

  return names.map((name) => {
    const a = beforeByName.get(name) ?? null;
    const b = afterByName.get(name) ?? null;

    const change: ProfileChange =
      a === null ? "added" : b === null ? "removed" : a === b ? "unchanged" : "changed";

    return { name, change, before: a, after: b };
  });
};

export const VELOCITY_RESULT_TYPE = "Number" as const;
