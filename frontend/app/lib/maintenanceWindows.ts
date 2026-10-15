import type { MaintenanceStatus, MaintenanceWindow } from "@/types";

export const MINIMUM_NOTICE_HOURS = 24;

const HOUR_MS = 3_600_000;

export const noticeHours = (
  window: Pick<MaintenanceWindow, "announcedAt" | "scheduledStart">
): number | null => {
  if (window.announcedAt === null) return null;

  const announced = Date.parse(window.announcedAt);
  const start = Date.parse(window.scheduledStart);

  if (Number.isNaN(announced) || Number.isNaN(start)) return null;

  return (start - announced) / HOUR_MS;
};

export const isProperlyAnnounced = (
  window: Pick<MaintenanceWindow, "announcedAt" | "scheduledStart">
): boolean | null => {
  const hours = noticeHours(window);

  return hours === null ? null : hours >= MINIMUM_NOTICE_HOURS;
};

export const formatNotice = (hours: number): string => {
  if (hours < 0) {
    return `${formatNotice(Math.abs(hours))} after it started`;
  }

  if (hours < 1) {
    const minutes = Math.round(hours * 60);
    return `${minutes} minute${minutes === 1 ? "" : "s"}`;
  }

  if (hours < 48) {
    const rounded = Math.round(hours);
    return `${rounded} hour${rounded === 1 ? "" : "s"}`;
  }

  const days = Math.round(hours / 24);

  return `${days} day${days === 1 ? "" : "s"}`;
};

export const noticeSentence = (
  window: Pick<MaintenanceWindow, "announcedAt" | "scheduledStart">
): string => {
  const hours = noticeHours(window);

  if (hours === null) {
    return "No announcement is recorded against this window, so there is no way to tell whether its hours are excluded from availability.";
  }

  if (hours >= MINIMUM_NOTICE_HOURS) {
    return `Announced ${formatNotice(hours)} ahead. That clears the ${MINIMUM_NOTICE_HOURS}-hour minimum, so these hours are excluded from availability targets.`;
  }

  const short = MINIMUM_NOTICE_HOURS - hours;

  return `Announced only ${formatNotice(hours)} ahead — ${formatNotice(
    short
  )} short of the ${MINIMUM_NOTICE_HOURS}-hour minimum. These hours count against every customer's availability target exactly as an unplanned outage would.`;
};

export const plannedNotice = (
  scheduledStart: string,
  now: Date = new Date()
): { clears: boolean; sentence: string } | null => {
  if (scheduledStart.trim().length === 0) return null;

  const start = Date.parse(scheduledStart);
  if (Number.isNaN(start)) return null;

  const hours = (start - now.getTime()) / HOUR_MS;

  if (hours <= 0) {
    return {
      clears: false,
      sentence:
        "That start is in the past. Announcing a window after it has begun gives no notice at all, and every hour of it counts against availability.",
    };
  }

  if (hours >= MINIMUM_NOTICE_HOURS) {
    return {
      clears: true,
      sentence: `Saving now announces this ${formatNotice(
        hours
      )} ahead, which clears the ${MINIMUM_NOTICE_HOURS}-hour minimum. The window's hours will be excluded from availability targets.`,
    };
  }

  return {
    clears: false,
    sentence: `Saving now announces this only ${formatNotice(
      hours
    )} ahead, under the ${MINIMUM_NOTICE_HOURS}-hour minimum. The window's hours will count against every customer's availability target as though the service had simply failed.`,
  };
};

export const MAINTENANCE_STATUSES: MaintenanceStatus[] = [
  "Scheduled",
  "InProgress",
  "Completed",
  "Cancelled",
];

export const MAINTENANCE_STATUS_LABELS: Record<MaintenanceStatus, string> = {
  Scheduled: "Scheduled",
  InProgress: "In progress",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

export const MAINTENANCE_STATUS_DESCRIPTIONS: Record<MaintenanceStatus, string> =
  {
    Scheduled: "Announced, and still ahead of us.",
    InProgress:
      "Running now. Moved here by the clock rather than by anybody pressing anything.",
    Completed: "The window has passed. Also moved by the clock.",
    Cancelled:
      "Called off before it started. Kept rather than deleted — customers planned around it, and withdrawing it silently is worse than announcing that it was withdrawn.",
  };

export const canEdit = (window: Pick<MaintenanceWindow, "status">): boolean =>
  window.status === "Scheduled";

export const canCancel = (window: Pick<MaintenanceWindow, "status">): boolean =>
  window.status === "Scheduled";

export const editRefusal = (
  window: Pick<MaintenanceWindow, "status">
): string | null => {
  switch (window.status) {
    case "Scheduled":
      return null;
    case "InProgress":
      return "This window is running. It can no longer be moved — customers are already in it.";
    case "Completed":
      return "This window has passed. What happened is a matter of record now.";
    case "Cancelled":
      return "This window was cancelled. Schedule a new one rather than reviving it, so subscribers are told about the new dates.";
    default:
      return "This window can no longer be changed.";
  }
};

export const TITLE_MAX_LENGTH = 200;
export const DESCRIPTION_MAX_LENGTH = 2000;

export const maintenanceRefusal = (draft: {
  title: string;
  description: string;
  components: readonly string[];
  scheduledStart: string;
  scheduledEnd: string;
}): string | null => {
  if (draft.title.trim().length === 0) {
    return "Give it a title. It goes to every confirmed subscriber.";
  }

  if (draft.title.trim().length > TITLE_MAX_LENGTH) {
    return `A title is at most ${TITLE_MAX_LENGTH} characters.`;
  }

  if (draft.description.trim().length === 0) {
    return "Describe what is being done and what customers should expect while it runs.";
  }

  if (draft.description.trim().length > DESCRIPTION_MAX_LENGTH) {
    return `A description is at most ${DESCRIPTION_MAX_LENGTH} characters.`;
  }

  if (draft.components.length === 0) {
    return "Name at least one component. A window over nothing tells a customer their integration is fine when it is about to stop.";
  }

  if (
    draft.scheduledStart.trim().length === 0 ||
    draft.scheduledEnd.trim().length === 0
  ) {
    return "Both a start and an end are needed. A window with no end is an outage.";
  }

  const start = Date.parse(draft.scheduledStart);
  const end = Date.parse(draft.scheduledEnd);

  if (Number.isNaN(start) || Number.isNaN(end)) {
    return "One of those dates could not be read.";
  }

  if (end <= start) {
    return "The window ends at or before it starts.";
  }

  return null;
};

export const soonestFirst = (
  windows: readonly MaintenanceWindow[]
): MaintenanceWindow[] =>
  [...windows].sort(
    (a, b) => Date.parse(a.scheduledStart) - Date.parse(b.scheduledStart)
  );

export const isRunning = (
  window: Pick<MaintenanceWindow, "scheduledStart" | "scheduledEnd">,
  now: Date = new Date()
): boolean => {
  const start = Date.parse(window.scheduledStart);
  const end = Date.parse(window.scheduledEnd);
  const at = now.getTime();

  return at >= start && at < end;
};

export const windowLength = (
  window: Pick<MaintenanceWindow, "scheduledStart" | "scheduledEnd">
): string => {
  const start = Date.parse(window.scheduledStart);
  const end = Date.parse(window.scheduledEnd);

  if (Number.isNaN(start) || Number.isNaN(end) || end <= start) return "—";

  const minutes = Math.round((end - start) / 60_000);

  if (minutes < 60) return `${minutes} minute${minutes === 1 ? "" : "s"}`;

  const hours = Math.round((minutes / 60) * 10) / 10;

  return `${hours} hour${hours === 1 ? "" : "s"}`;
};

export const CREATING_IS_ANNOUNCING =
  "Saving this announces it. Every confirmed subscriber is mailed, and it appears on the public status page immediately.";

export const MOVING_REDATES_THE_ANNOUNCEMENT =
  "Moving a window re-dates its announcement, so the notice period is measured again from now — a week's notice followed by a move to tomorrow is a day's notice, not a week's.";

export const CANCELLING_KEEPS_THE_RECORD =
  "Cancelled rather than deleted. Customers planned around this window, so it stays on the record as something that was announced and then withdrawn.";
