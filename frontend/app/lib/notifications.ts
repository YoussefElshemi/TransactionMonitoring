import { NotificationChannel, NotificationSubscription, RuleSeverity } from "@/types";

export const CHANNEL_LABEL: Record<NotificationChannel, string> = {
  Email: "Email",
  Chat: "Chat",
};

export const CHANNEL_HINT: Record<NotificationChannel, string> = {
  Email: "A mailbox. One person, or a shared desk address.",
  Chat: "A Slack or Teams incoming-webhook URL. The message lands in the channel that URL belongs to.",
};

export const DESTINATION_LABEL: Record<NotificationChannel, string> = {
  Email: "Email address",
  Chat: "Incoming webhook URL",
};

export const DESTINATION_PLACEHOLDER: Record<NotificationChannel, string> = {
  Email: "ops@example.com",
  Chat: "https://hooks.slack.com/services/…",
};

export const DESTINATION_INPUT_TYPE: Record<NotificationChannel, string> = {
  Email: "email",
  Chat: "url",
};

export const MAX_DESTINATION = 512;

export const destinationProblem = (
  channel: NotificationChannel,
  destination: string
): string | null => {
  const value = destination.trim();

  if (!value) return "A destination is required.";
  if (value.length > MAX_DESTINATION) {
    return `That is ${value.length} characters. The limit is ${MAX_DESTINATION}.`;
  }

  if (channel === "Email") {
    return /^[^\s@]+@[^\s@.]+(\.[^\s@.]+)+$/.test(value)
      ? null
      : "That does not look like an email address.";
  }

  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:"
      ? null
      : "The URL has to start with http:// or https://.";
  } catch {
    return "That is not a URL. Paste the whole incoming-webhook address.";
  }
};

export const asksForSomething = (subscription: {
  minimumSeverity: RuleSeverity | null;
  notifyOnBreach: boolean;
}): boolean =>
  subscription.minimumSeverity !== null || subscription.notifyOnBreach;

export const severityTriggerLabel = (
  minimumSeverity: RuleSeverity | null
): string =>
  minimumSeverity === null ? "Every severity" : `${minimumSeverity} and above`;

export const SEVERITY_TRIGGER_HELP =
  "Fires when a case opens at or above this band. A case's severity is set by the finding that opened it and is never revised, so a case that gathers a worse alert later does not fire this again.";

export const BREACH_TRIGGER_HELP =
  "Fires when a case passes its deadline. This ignores the severity floor above, on purpose: the low-severity work is what sits untouched longest, and it is exactly what a breach alert is for.";

export const BREACH_SWEEP_NOTE =
  "Breaches are swept about once a minute, and each case is notified once.";

export const triggerChips = (
  subscription: Pick<
    NotificationSubscription,
    "minimumSeverity" | "notifyOnBreach"
  >
): string[] => {
  const chips = [severityTriggerLabel(subscription.minimumSeverity)];
  if (subscription.notifyOnBreach) chips.push("On breach");
  return chips;
};

export const submitProblem = (draft: {
  channel: NotificationChannel;
  destination: string;
  minimumSeverity: RuleSeverity | null;
  notifyOnBreach: boolean;
}): string | null => {
  const destination = destinationProblem(draft.channel, draft.destination);
  if (destination) return destination;

  if (!asksForSomething(draft)) {
    return "This would ask for nothing. Set a severity floor, turn on breach notifications, or both.";
  }

  return null;
};

export const SUBSCRIPTION_FIELD_LABELS: Record<string, string> = {
  "Body.Channel": "Channel",
  "Body.Destination": DESTINATION_LABEL.Email,
  "Body.MinimumSeverity": "Minimum severity",
  "Body.NotifyOnBreach": "Breach notifications",
  "Body.IsEnabled": "Enabled",
  Body: "",
};
