import prettyMilliseconds from "pretty-ms";

import { Case, CaseAgeBucket, CaseAgeing } from "@/types";
import { isTerminalCase } from "@/lib/cases";

import { formatInstant } from "@/lib/format";

export const AGE_BUCKETS: CaseAgeBucket[] = [
  "Fresh",
  "Ageing",
  "DueSoon",
  "Breached",
];

const BUCKET_LABELS: Record<string, string> = {
  Fresh: "Fresh",
  Ageing: "Ageing",
  DueSoon: "Due soon",
  Breached: "Breached",
};

export const bucketLabel = (bucket: string): string =>
  BUCKET_LABELS[bucket] ?? bucket;

export const BUCKET_HINTS: Record<CaseAgeBucket, string> = {
  Fresh: "Less than half of this case's window has gone.",
  Ageing: "Between half and four fifths of its window has gone.",
  DueSoon: "Four fifths or more of its window has gone, and it is not late yet.",
  Breached: "Past its deadline. An open case here is late right now.",
};

export const durationLabel = (milliseconds: number): string =>
  prettyMilliseconds(Math.abs(milliseconds), {
    unitCount: 2,
    secondsDecimalDigits: 0,
  });

export interface DuePhrase {
  text: string;
  short: string;
  overdue: boolean;
}

export const duePhrase = (timeToDueInMilliseconds: number): DuePhrase => {
  const overdue = timeToDueInMilliseconds < 0;
  const label = durationLabel(timeToDueInMilliseconds);

  return {
    text: overdue ? `overdue by ${label}` : `due in ${label}`,
    short: overdue ? `${label} late` : `${label} left`,
    overdue,
  };
};

export const deadlineTitle = (
  dueAt: string,
  locale: string,
  timeZone: string
): string =>
  `Due ${formatInstant(
    dueAt,
    locale,
    timeZone,
    "deadline"
  )} — the earlier of this severity's SLA target and the hold expiring underneath, fixed when the case opened.`;

export const isAgeingFrozen = (subject: {
  status: string;
  resolvedAt: string | null;
}): boolean => isTerminalCase(subject.status) && subject.resolvedAt !== null;

export const tickAgeing = (
  ageing: CaseAgeing,
  elapsedMilliseconds: number,
  frozen: boolean
): CaseAgeing => {
  if (frozen || elapsedMilliseconds <= 0) return ageing;

  return {
    ...ageing,
    ageInMilliseconds: ageing.ageInMilliseconds + elapsedMilliseconds,
    timeToDueInMilliseconds:
      ageing.timeToDueInMilliseconds - elapsedMilliseconds,
  };
};

export const AGEING_REFRESH_MS = 30_000;

export const isRuleEscalated = (subject: {
  escalatedAt: string | null;
}): boolean => subject.escalatedAt !== null;

export interface EscalationStamp {
  escalatedAt: string;
  escalationRuleId: string | null;
}

export const escalationStamp = (subject: Case): EscalationStamp | null =>
  subject.escalatedAt === null
    ? null
    : {
        escalatedAt: subject.escalatedAt,
        escalationRuleId: subject.escalationRuleId,
      };
