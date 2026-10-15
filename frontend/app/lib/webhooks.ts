import type {
  WebhookConfiguration,
  WebhookDeliveryResponse,
  WebhookEndpointHealth,
  WebhookEventCategory,
  WebhookEventDefinition,
} from "@/types";
import type { Tone } from "@/components/status/StatusChips";

export const EVENT_CATEGORY_ORDER: WebhookEventCategory[] = [
  "Case",
  "Decision",
  "Alert",
  "Approval",
  "SuspiciousActivityReport",
  "Diagnostic",
];

export const EVENT_CATEGORY_LABELS: Record<WebhookEventCategory, string> = {
  Case: "Cases",
  Decision: "Decisions",
  Alert: "Alerts",
  Approval: "Approvals",
  SuspiciousActivityReport: "Filings",
  Diagnostic: "Diagnostics",
};

export const eventCategoryLabel = (category: string): string =>
  EVENT_CATEGORY_LABELS[category as WebhookEventCategory] ?? category;

export interface EventCategoryGroup {
  category: string;
  label: string;
  events: WebhookEventDefinition[];
}

export const groupEventsByCategory = (
  events: readonly WebhookEventDefinition[]
): EventCategoryGroup[] => {
  const byCategory = new Map<string, WebhookEventDefinition[]>();

  for (const definition of events) {
    const existing = byCategory.get(definition.category);
    if (existing) existing.push(definition);
    else byCategory.set(definition.category, [definition]);
  }

  const known = EVENT_CATEGORY_ORDER.filter((category) =>
    byCategory.has(category)
  ) as string[];
  const unknown = [...byCategory.keys()].filter(
    (category) => !EVENT_CATEGORY_ORDER.includes(category as WebhookEventCategory)
  );

  return [...known, ...unknown].map((category) => ({
    category,
    label: eventCategoryLabel(category),
    events: byCategory.get(category) ?? [],
  }));
};

export const subscribableEvents = (
  events: readonly WebhookEventDefinition[]
): WebhookEventDefinition[] => events.filter((event) => event.isSubscribable);

export const notSubscribableReason = (
  definition: WebhookEventDefinition
): string | null =>
  definition.isSubscribable
    ? null
    : "Sent only by this endpoint's Test button, and delivered whatever the endpoint subscribes to — so there is nothing to subscribe to.";

export const EVERY_EVENT =
  "Every event, including any added later. Selecting nothing is how an endpoint asks for everything — it is not an empty subscription.";

export const EVERY_EVENT_SHORT = "All events";

export const subscribesToEverything = (
  configuration: Pick<WebhookConfiguration, "events">
): boolean => configuration.events.length === 0;

export const subscriptionSummary = (
  configuration: Pick<WebhookConfiguration, "events">,
  catalogueSize?: number
): string => {
  if (subscribesToEverything(configuration)) return EVERY_EVENT_SHORT;

  const count = configuration.events.length;
  const noun = `event${count === 1 ? "" : "s"}`;

  return catalogueSize && catalogueSize > 0
    ? `${count} of ${catalogueSize} ${noun}`
    : `${count} ${noun}`;
};

export const wideningWarning = (
  original: readonly string[],
  next: readonly string[]
): string | null =>
  original.length > 0 && next.length === 0
    ? "Clearing the last event widens this endpoint back out to every event, which is what an empty subscription means. Disable the endpoint instead if you want it to stop receiving."
    : null;

// Omitting events leaves the subscription alone; sending [] widens it to every event.
export const eventsPatchValue = (
  original: readonly string[],
  next: readonly string[]
): string[] | undefined => {
  const before = [...original].sort();
  const after = [...next].sort();

  const changed =
    before.length !== after.length ||
    before.some((event, index) => event !== after[index]);

  return changed ? [...next] : undefined;
};

export type WebhookDisableState =
  | "enabled"
  | "paused"
  | "auto-disabled";

export const disableState = (
  configuration: Pick<WebhookConfiguration, "isEnabled" | "disabledAt">
): WebhookDisableState => {
  if (configuration.isEnabled) return "enabled";
  return configuration.disabledAt === null ? "paused" : "auto-disabled";
};

export const DISABLE_STATE_LABELS: Record<WebhookDisableState, string> = {
  enabled: "Enabled",
  paused: "Paused",
  "auto-disabled": "Switched off",
};

export const disableStateTone = (state: WebhookDisableState): Tone => {
  switch (state) {
    case "enabled":
      return "settled";
    case "paused":
      return "inert";
    case "auto-disabled":
      return "critical";
  }
};

export const DISABLE_STATE_SENTENCES: Record<WebhookDisableState, string> = {
  enabled: "Deliveries are being posted to this endpoint.",
  paused:
    "Somebody turned this endpoint off. Nothing is wrong with it, and nothing is being queued for it while it is off.",
  "auto-disabled":
    "The platform switched this endpoint off itself after it failed too many deliveries in a row. It stays off until somebody enables it again.",
};

export const RE_ENABLE_RECOVERS =
  "Enabling clears the automatic switch-off and resets the failure count to zero. Fix the endpoint first — if it is still failing, it will be switched off again.";

export const healthTone = (health: WebhookEndpointHealth): Tone => {
  if (health.disabledAt !== null) return "critical";
  if (!health.isEnabled) return "inert";
  if (health.consecutiveFailures > 0) return "attention";
  return "settled";
};

export const healthSentence = (health: WebhookEndpointHealth): string => {
  if (health.disabledAt !== null) {
    return DISABLE_STATE_SENTENCES["auto-disabled"];
  }

  if (!health.isEnabled) return DISABLE_STATE_SENTENCES.paused;

  if (health.consecutiveFailures > 0) {
    return `The last ${health.consecutiveFailures} ${
      health.consecutiveFailures === 1 ? "delivery has" : "deliveries have"
    } failed in a row. Enough of these and the platform switches the endpoint off by itself.`;
  }

  return health.lastSucceededAt === null
    ? "Nothing has been delivered to this endpoint yet. Use Test to prove it is reachable."
    : "The last delivery to this endpoint succeeded.";
};

export const windowLength = (
  window: Pick<WebhookEndpointHealth, "from" | "to">
): string => {
  const from = Date.parse(window.from);
  const to = Date.parse(window.to);

  if (Number.isNaN(from) || Number.isNaN(to) || to <= from) return "the window";

  const hours = Math.round((to - from) / 3_600_000);
  if (hours < 48) return `${hours} hour${hours === 1 ? "" : "s"}`;

  const days = Math.round(hours / 24);
  return `${days} day${days === 1 ? "" : "s"}`;
};

export const PENDING_MEANS_WAITING =
  "Queued and still inside its retry budget. These need waiting for, not investigating — a failed one has run out of retries.";

export type DeliveryOutcome =
  | "delivered"
  | "suppressed"
  | "undelivered";

export const deliveryOutcome = (
  delivery: Pick<WebhookDeliveryResponse, "isDelivered" | "suppressedAt">
): DeliveryOutcome => {
  if (delivery.suppressedAt !== null) return "suppressed";
  return delivery.isDelivered ? "delivered" : "undelivered";
};

export const DELIVERY_OUTCOME_LABELS: Record<DeliveryOutcome, string> = {
  delivered: "Delivered",
  suppressed: "Suppressed",
  undelivered: "Not delivered",
};

export const deliveryOutcomeTone = (outcome: DeliveryOutcome): Tone => {
  switch (outcome) {
    case "delivered":
      return "settled";
    case "undelivered":
      return "attention";
    case "suppressed":
      return "inert";
  }
};

export const DELIVERY_OUTCOME_SENTENCES: Record<DeliveryOutcome, string> = {
  delivered: "The endpoint answered successfully.",
  undelivered:
    "Not accepted yet. It may still be inside its retry budget, or it may have run out of attempts — the response below says which is more likely. Replaying it sends the same message again.",
  suppressed:
    "Never sent. The endpoint was switched off, or this is not an event it subscribes to.",
};

export const responseStatusLabel = (
  delivery: Pick<WebhookDeliveryResponse, "responseStatusCode">
): string =>
  delivery.responseStatusCode === null
    ? "No answer"
    : String(delivery.responseStatusCode);

export const NO_ANSWER_MEANS =
  "The request never got an answer — a timeout, a refused connection, or a hostname that did not resolve. The response body below is the transport error, not something the endpoint said.";

export const responseStatusTone = (
  delivery: Pick<WebhookDeliveryResponse, "responseStatusCode">
): Tone => {
  const status = delivery.responseStatusCode;

  if (status === null) return "critical";
  if (status >= 200 && status < 300) return "settled";
  if (status >= 500) return "critical";

  return "attention";
};

export const formatLatency = (
  milliseconds: number | null | undefined
): string => {
  if (milliseconds === null || milliseconds === undefined) return "—";
  if (milliseconds < 1000) return `${milliseconds} ms`;

  return `${(milliseconds / 1000).toFixed(milliseconds < 10_000 ? 1 : 0)} s`;
};

export const parsePayload = (payload: string): unknown | null => {
  try {
    return JSON.parse(payload);
  } catch {
    return null;
  }
};

export const REPLAY_SENDS_AGAIN =
  "Queues the same message again, to wherever the endpoint points now. The original row is left as it is, so a replayed event appears twice in this log on purpose.";

export const REPLAY_WINDOW_EXPLAINS =
  "Resends everything the window holds. Use it after fixing an endpoint that was down — the events it missed are already recorded here, they just were not accepted.";

export const INCLUDE_DELIVERED_EXPLAINS =
  "Off by default, so catching up after an outage does not also resend everything the endpoint already processed either side of it.";

export const replayWindowRefusal = (window: {
  from: string;
  to: string;
}): string | null => {
  if (!window.from || !window.to) {
    return "Name both ends of the window. An unbounded replay is every delivery this organisation has ever made.";
  }

  const from = Date.parse(window.from);
  const to = Date.parse(window.to);

  if (Number.isNaN(from) || Number.isNaN(to)) {
    return "That is not a date the API will accept.";
  }

  if (to <= from) {
    return "The window ends before it starts.";
  }

  return null;
};

export const replayBatchSentence = (batch: {
  matched: number;
  queued: number;
  hasMore: boolean;
}): string => {
  const head = `${batch.matched} ${
    batch.matched === 1 ? "delivery" : "deliveries"
  } matched, ${batch.queued} queued.`;

  if (batch.hasMore) {
    return `${head} The batch hit its ceiling before the window ran out — run it again over the part that is left.`;
  }

  if (batch.matched === 0) {
    return "Nothing in that window matched. Widen it, or turn on including deliveries that already succeeded.";
  }

  return `${head} The whole window is queued.`;
};

export const NO_SOURCE_RANGES =
  "None published. This installation has not declared the addresses it sends from, so an allowlist cannot be written against it — ask whoever runs it before putting one in front of your endpoint.";

export const PAYLOAD_VERSION_PINS =
  "The envelope version this endpoint receives. Pinning one means a later version does not arrive at a parser that has not been updated for it.";
