import type {
  ComponentHealth,
  ComponentStatus,
  ComponentUptimeDay,
  ServiceComponent,
  ServiceStatus,
} from "@/types";

export const SERVICE_COMPONENTS: ServiceComponent[] = [
  "Api",
  "RuleEvaluation",
  "Webhooks",
  "BackgroundProcessing",
  "Reporting",
];

export const COMPONENT_LABELS: Record<ServiceComponent, string> = {
  Api: "API",
  RuleEvaluation: "Rule evaluation",
  Webhooks: "Webhooks",
  BackgroundProcessing: "Background processing",
  Reporting: "Reporting",
};

export const COMPONENT_DESCRIPTIONS: Record<ServiceComponent, string> = {
  Api: "The HTTP surface itself: everything a client calls, and the database round trip behind it.",
  RuleEvaluation:
    "The authorisation path — the decision that sits in front of a transaction.",
  Webhooks:
    "Outbound delivery. A queue that is not draining is ours; an endpoint that is down is yours.",
  BackgroundProcessing:
    "The scheduled work behind deadlines, escalations, invoicing and filings.",
  Reporting: "Statistics, exports, and the documents assembled from them.",
};

export const componentLabel = (component: ServiceComponent | string): string =>
  COMPONENT_LABELS[component as ServiceComponent] ?? String(component);

export const componentsSentence = (
  components: readonly (ServiceComponent | string)[]
): string => {
  const labels = components.map(componentLabel);

  if (labels.length === 0) return "no components";
  if (labels.length === 1) return labels[0];

  return `${labels.slice(0, -1).join(", ")} and ${labels[labels.length - 1]}`;
};

export const COMPONENT_STATUS_LABELS: Record<ComponentStatus, string> = {
  Operational: "Operational",
  Degraded: "Degraded",
  PartialOutage: "Partial outage",
  MajorOutage: "Major outage",
  UnderMaintenance: "Under maintenance",
};

export const COMPONENT_STATUS_DESCRIPTIONS: Record<ComponentStatus, string> = {
  Operational: "Answering normally.",
  Degraded:
    "Answering, but slower than it promised — slow enough to break a caller's timeout.",
  PartialOutage: "Some requests are failing.",
  MajorOutage: "Requests are failing.",
  UnderMaintenance:
    "Announced downtime. Planned, and it does not count against an availability target.",
};

export const componentStatusLabel = (status: ComponentStatus): string =>
  COMPONENT_STATUS_LABELS[status] ?? String(status);

export const isOperational = (status: ComponentStatus): boolean =>
  status === "Operational";

export const isFailing = (status: ComponentStatus): boolean =>
  status === "Degraded" ||
  status === "PartialOutage" ||
  status === "MajorOutage";

export const componentStatusTone = (
  status: ComponentStatus
): "settled" | "inflight" | "attention" | "elevated" | "critical" => {
  switch (status) {
    case "Operational":
      return "settled";
    case "UnderMaintenance":
      return "inflight";
    case "Degraded":
      return "attention";
    case "PartialOutage":
      return "elevated";
    case "MajorOutage":
      return "critical";
    default:
      return "settled";
  }
};

export const overallHeadline = (status: ComponentStatus): string => {
  switch (status) {
    case "Operational":
      return "All systems operational";
    case "Degraded":
      return "Some systems are degraded";
    case "PartialOutage":
      return "Partial outage";
    case "MajorOutage":
      return "Major outage";
    case "UnderMaintenance":
      return "Under maintenance";
    default:
      return String(status);
  }
};

export const overallDetail = (status: ServiceStatus): string => {
  if (status.status === "Operational") {
    return "Every component is answering normally.";
  }

  const affected = status.components.filter(
    (component) => component.status !== "Operational"
  );

  if (affected.length === 0) {
    return "The service is reporting a problem that no individual component is showing. Treat the component rows below as the more reliable answer.";
  }

  const maintenance = affected.filter(
    (component) => component.status === "UnderMaintenance"
  );
  const failing = affected.filter((component) => isFailing(component.status));

  if (failing.length === 0) {
    return `${componentsSentence(
      maintenance.map((component) => component.component)
    )} ${maintenance.length === 1 ? "is" : "are"} in announced maintenance. Nothing else is affected.`;
  }

  const names = componentsSentence(
    failing.map((component) => component.component)
  );

  return `${names} ${failing.length === 1 ? "is" : "are"} affected. Everything else is answering normally.`;
};

export type UptimeDayReading = "unknown" | "up" | "degraded" | "down";

export const hasNoProbeData = (day: ComponentUptimeDay): boolean =>
  day.totalProbes <= 0;

export const DEGRADED_DAY_THRESHOLD = 99;

export const DOWN_DAY_THRESHOLD = 95;

export const readUptimeDay = (day: ComponentUptimeDay): UptimeDayReading => {
  if (hasNoProbeData(day)) return "unknown";
  if (day.uptimePercentage < DOWN_DAY_THRESHOLD) return "down";
  if (day.uptimePercentage < DEGRADED_DAY_THRESHOLD) return "degraded";

  return "up";
};

export const uptimeDayTitle = (day: ComponentUptimeDay): string => {
  if (hasNoProbeData(day)) {
    return `${day.date} — no data. Nothing probed this component that day, so nothing is known about it. It is not being reported as up.`;
  }

  const uptime = formatUptime(day.uptimePercentage);
  const probes = `${day.healthyProbes.toLocaleString("en-GB")} of ${day.totalProbes.toLocaleString(
    "en-GB"
  )} probes healthy`;

  if (day.downtimeSeconds <= 0) return `${day.date} — ${uptime}, ${probes}`;

  return `${day.date} — ${uptime}, ${probes}, ${formatDowntime(
    day.downtimeSeconds
  )} down`;
};

export const uptimeWindow = (
  history: readonly ComponentUptimeDay[],
  days = 90
): (ComponentUptimeDay | null)[] => {
  const recent = history.slice(-days);
  const missing = Math.max(0, days - recent.length);

  return [...Array<null>(missing).fill(null), ...recent];
};

export const probedDayCount = (
  history: readonly ComponentUptimeDay[]
): number => history.filter((day) => !hasNoProbeData(day)).length;

export const uptimeCoverageNote = (
  history: readonly ComponentUptimeDay[],
  days = 90
): string | null => {
  const probed = probedDayCount(history);
  if (probed >= days) return null;

  if (probed === 0) {
    return `Nothing has been probed in the last ${days} days. The bar is empty because nothing is known, not because the service was down.`;
  }

  return `${probed} of the last ${days} days were probed. The rest are gaps in the monitoring, not outages.`;
};

export const formatUptime = (percentage: number): string => {
  const fixed = percentage.toFixed(4);
  const trimmed = fixed.replace(/\.?0+$/, "");

  return `${trimmed}%`;
};

export const formatDowntime = (seconds: number): string => {
  if (seconds < 60) {
    return `${seconds} second${seconds === 1 ? "" : "s"}`;
  }

  const minutes = Math.round(seconds / 60);
  if (minutes < 60) return `${minutes} minute${minutes === 1 ? "" : "s"}`;

  const hours = seconds / 3600;
  const rounded = Math.round(hours * 10) / 10;

  return `${rounded} hour${rounded === 1 ? "" : "s"}`;
};

export const formatLatency = (milliseconds: number | null): string =>
  milliseconds === null ? "never probed" : `${milliseconds} ms`;

export const isPlausibleEmail = (email: string): boolean => {
  const trimmed = email.trim();

  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed) && trimmed.length <= 320;
};

export const SUBSCRIBE_ACKNOWLEDGEMENT =
  "If that address can receive mail, a confirmation has been sent to it. Follow the link in it to start receiving updates.";

export const SUBSCRIBE_HINT =
  "Get an email when an incident is opened or updated, and when maintenance is scheduled. One click to unsubscribe, on every message.";

export const componentsInOrder = (
  components: readonly ComponentHealth[]
): ComponentHealth[] =>
  [...components].sort(
    (a, b) =>
      SERVICE_COMPONENTS.indexOf(a.component) -
      SERVICE_COMPONENTS.indexOf(b.component)
  );

export const STATUS_POLL_INTERVAL_MS = 60_000;

export const STATUS_IS_DERIVED =
  "Component status is worked out when you load this page, from the last probes, any open incident and any announced maintenance. Nobody sets it by hand.";
