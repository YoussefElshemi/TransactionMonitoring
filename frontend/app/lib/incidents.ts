import type { Incident, IncidentImpact, IncidentStatus, IncidentUpdate } from "@/types";

export const INCIDENT_STATUSES: IncidentStatus[] = [
  "Investigating",
  "Identified",
  "Monitoring",
  "Resolved",
];

export const INCIDENT_STATUS_DESCRIPTIONS: Record<IncidentStatus, string> = {
  Investigating: "Something is wrong and the cause is not yet known.",
  Identified: "The cause is known. A fix is being worked on.",
  Monitoring:
    "The fix is in and is being watched. Not over — declaring it over and being wrong costs more than leaving it open another hour.",
  Resolved:
    "Over. Posting this update is what resolves the incident and closes the public timeline.",
};

export const incidentStatusTone = (
  status: IncidentStatus
): "settled" | "inflight" | "attention" | "elevated" => {
  switch (status) {
    case "Resolved":
      return "settled";
    case "Monitoring":
      return "inflight";
    case "Identified":
      return "elevated";
    case "Investigating":
      return "attention";
    default:
      return "attention";
  }
};

export const isResolved = (incident: Pick<Incident, "status">): boolean =>
  incident.status === "Resolved";

export const INCIDENT_IMPACTS: IncidentImpact[] = [
  "None",
  "Minor",
  "Major",
  "Critical",
];

export const INCIDENT_IMPACT_DESCRIPTIONS: Record<IncidentImpact, string> = {
  None: "Nothing a customer would notice. Worth recording anyway — somebody still had to respond to it, and the review is worth the same.",
  Minor: "Noticeable, but nothing failed for the customer.",
  Major: "Customers are affected. Requests are failing or are too slow to use.",
  Critical: "The service is unusable for the affected components.",
};

export const incidentImpactTone = (
  impact: IncidentImpact
): "settled" | "attention" | "elevated" | "critical" => {
  switch (impact) {
    case "None":
      return "settled";
    case "Minor":
      return "attention";
    case "Major":
      return "elevated";
    case "Critical":
      return "critical";
    default:
      return "attention";
  }
};

export const timelineNewestFirst = (
  updates: readonly IncidentUpdate[]
): IncidentUpdate[] =>
  [...updates].sort((a, b) => Date.parse(b.postedAt) - Date.parse(a.postedAt));

export const latestUpdate = (
  incident: Pick<Incident, "updates">
): IncidentUpdate | null => timelineNewestFirst(incident.updates)[0] ?? null;

export const incidentDurationMs = (
  incident: Pick<Incident, "startedAt" | "resolvedAt">,
  now: Date = new Date()
): number => {
  const started = Date.parse(incident.startedAt);
  if (Number.isNaN(started)) return 0;

  const ended =
    incident.resolvedAt === null
      ? now.getTime()
      : Date.parse(incident.resolvedAt);

  return Math.max(0, (Number.isNaN(ended) ? now.getTime() : ended) - started);
};

export const formatDuration = (milliseconds: number): string => {
  const minutes = Math.floor(milliseconds / 60_000);

  if (minutes < 1) return "under a minute";
  if (minutes < 60) return `${minutes}m`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    const remainder = minutes % 60;
    return remainder === 0 ? `${hours}h` : `${hours}h ${remainder}m`;
  }

  const days = Math.floor(hours / 24);
  const remainingHours = hours % 24;

  return remainingHours === 0
    ? `${days} day${days === 1 ? "" : "s"}`
    : `${days}d ${remainingHours}h`;
};

export const durationSentence = (
  incident: Pick<Incident, "startedAt" | "resolvedAt">,
  now: Date = new Date()
): string => {
  const phrase = formatDuration(incidentDurationMs(incident, now));

  return incident.resolvedAt === null
    ? `Open for ${phrase}, measured from when customers were first affected.`
    : `Ran for ${phrase}, from when customers were first affected to when it was resolved.`;
};

export const reviewRefusal = (
  incident: Pick<Incident, "status">
): string | null =>
  incident.status === "Resolved"
    ? null
    : "A review can only be published once the incident is resolved. A review of something still happening is a guess.";

export const hasPublishedReview = (
  incident: Pick<Incident, "reviewPublishedAt">
): boolean => incident.reviewPublishedAt !== null;

export const TITLE_MAX_LENGTH = 200;
export const SUMMARY_MAX_LENGTH = 2000;
export const UPDATE_BODY_MAX_LENGTH = 4000;
export const REVIEW_MAX_LENGTH = 4000;

export const incidentRefusal = (draft: {
  title: string;
  summary: string;
  components: readonly string[];
}): string | null => {
  if (draft.title.trim().length === 0) {
    return "Give it a title. It is the line customers see first.";
  }

  if (draft.title.trim().length > TITLE_MAX_LENGTH) {
    return `A title is at most ${TITLE_MAX_LENGTH} characters.`;
  }

  if (draft.summary.trim().length === 0) {
    return "Write the summary. It is what a customer reads first and it becomes the opening entry on the timeline.";
  }

  if (draft.summary.trim().length > SUMMARY_MAX_LENGTH) {
    return `A summary is at most ${SUMMARY_MAX_LENGTH} characters.`;
  }

  if (draft.components.length === 0) {
    return "Name at least one component. Component status is worked out from the incidents open against it, so an incident against nothing changes nothing on the public page.";
  }

  return null;
};

export const updateRefusal = (body: string): string | null => {
  if (body.trim().length === 0) {
    return "Say what has changed. An update with nothing in it is a mail to every subscriber that tells them nothing.";
  }

  if (body.trim().length > UPDATE_BODY_MAX_LENGTH) {
    return `An update is at most ${UPDATE_BODY_MAX_LENGTH} characters.`;
  }

  return null;
};

export const reviewContentRefusal = (draft: {
  rootCause: string;
  remediation: string;
}): string | null => {
  if (draft.rootCause.trim().length === 0) {
    return "A review with no cause in it is an apology.";
  }

  if (draft.remediation.trim().length === 0) {
    return "A review with no remediation in it is a promise that nothing will change.";
  }

  if (
    draft.rootCause.trim().length > REVIEW_MAX_LENGTH ||
    draft.remediation.trim().length > REVIEW_MAX_LENGTH
  ) {
    return `Each half of a review is at most ${REVIEW_MAX_LENGTH} characters.`;
  }

  return null;
};

export const RESOLVED_ENDS_IT =
  "Posting an update as Resolved is what resolves the incident — there is no separate resolve action. The public page stops showing it as ongoing the moment this is posted.";

export const UPDATE_IS_PUBLISHED =
  "This is published on the public status page and mailed to every confirmed subscriber. It cannot be edited or withdrawn afterwards.";

export const EDIT_CORRECTS_THE_ACCOUNT =
  "Editing corrects the account of the incident — its title, summary, impact, components and true start. It does not change any update already published, because those have already been sent.";

export const STARTED_AT_HINT =
  "When customers were first affected, which is usually earlier than when anybody noticed. This is what the uptime figure is measured from. Leave it blank for now.";

export const REVIEW_IS_PUBLIC =
  "Published on the public status page under this incident. Write it for the customer who was affected, not for the team that fixed it.";

export const incidentHeading = (
  incident: Pick<Incident, "incidentNumber" | "title">
): string => `${incident.incidentNumber} — ${incident.title}`;
