import type { CurrencyCode, Plan, PlanVersion, Subscription } from "@/types";

import { DOCUMENT_LOCALE } from "@/lib/format";

export type PlanOwnership = "catalogue" | "bespoke";

export const planOwnership = (plan: {
  organizationId: string | null;
}): PlanOwnership => (plan.organizationId === null ? "catalogue" : "bespoke");

export const OWNERSHIP_LABELS: Record<PlanOwnership, string> = {
  catalogue: "Catalogue",
  bespoke: "Bespoke",
};

export const OWNERSHIP_HINTS: Record<PlanOwnership, string> = {
  catalogue:
    "On the shared price list. Any organisation on the installation can be put on it, and repricing it changes what every future subscriber is quoted.",
  bespoke:
    "Negotiated with one customer. Only they can be put on it, and it is not offered to — or visible from — anybody else's subscribe form.",
};

export const OWNERSHIP_CHOICE_HINT =
  "Decided now and never afterwards — there is no endpoint that moves a plan between the two.";

export const NAME_MAX_LENGTH = 200;

export const DESCRIPTION_MAX_LENGTH = 1000;

export const isValidPlanName = (name: string): boolean => {
  const trimmed = name.trim();
  return trimmed.length > 0 && trimmed.length <= NAME_MAX_LENGTH;
};

export const isValidPlanDescription = (description: string): boolean =>
  description.trim().length <= DESCRIPTION_MAX_LENGTH;

export const NAME_HINT =
  "Uniquely named per owner, so a bespoke plan called Growth does not collide with the catalogue's. The name is what a customer sees on their own screen and what an invoice is read against, so it should survive being repriced — \"Growth\" rather than \"Growth 2026\".";

export const PLAN_EDITABLE_FIELDS =
  "Name, description and whether the plan is offered. The rate is not editable here and never will be — repricing publishes a new version, which leaves every invoice already issued still pointing at the rate it was priced from.";

export const isValidCostPerRequest = (value: number): boolean =>
  Number.isFinite(value) && value >= 0;

export const formatRate = (
  costPerRequest: number,
  currency: CurrencyCode
): string =>
  costPerRequest.toLocaleString("en-GB", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  });

export const rateSentence = (
  costPerRequest: number,
  currency: CurrencyCode
): string => `${formatRate(costPerRequest, currency)} per request`;

export const HEADLINE_RATE_IS_NOT_THE_PRICE =
  "The headline rate: the catch-all price's bottom band, restated so the plan can be quoted in one figure. The invoice is computed from the bands, the platform fee and the monthly minimum — not from this.";

export const versionLabel = (
  planName: string | null,
  versionNumber: number | null
): string =>
  planName === null
    ? versionNumber === null
      ? "Unknown plan"
      : `Unknown plan v${versionNumber}`
    : versionNumber === null
      ? planName
      : `${planName} v${versionNumber}`;

export const VERSIONS_ARE_IMMUTABLE =
  "A published version is never changed. An invoice a customer already holds names the version it was priced from, so editing one would rewrite the arithmetic on a document that has already gone out.";

export const REPRICING_MOVES_NOBODY =
  "Publishing a version does not move anyone. Every organisation already subscribed stays on the version they were sold until they are put on the new one, each with its own effective date.";

export const DISABLE_RATHER_THAN_DELETE =
  "Disabling withdraws the plan from the catalogue: nobody new can be put on it, and everybody already on it stays on it and keeps being invoiced. This is almost always what is meant by retiring a price.";

export const DELETE_IS_NARROW =
  "Only possible for a plan nobody has ever been subscribed to — a mistake, or a price that was never sold. The API refuses to delete anything a subscription points at, because the months invoiced under it would be left naming nothing.";

export const PLAN_IN_USE_MESSAGE =
  "Somebody is on this plan, so it can't be deleted — the invoices raised under it name its versions. Disable it instead: that withdraws it from the catalogue and leaves the record intact.";

export type SubscriptionState = "scheduled" | "inForce" | "superseded";

export const isScheduled = (
  subscription: Pick<Subscription, "effectiveFrom">,
  now: Date = new Date()
): boolean => Date.parse(subscription.effectiveFrom) > now.getTime();

export const isInForce = (
  subscription: Pick<Subscription, "effectiveFrom" | "effectiveTo">,
  now: Date = new Date()
): boolean => {
  const from = Date.parse(subscription.effectiveFrom);
  if (from > now.getTime()) return false;

  return (
    subscription.effectiveTo === null ||
    Date.parse(subscription.effectiveTo) > now.getTime()
  );
};

export const isSuperseded = (
  subscription: Pick<Subscription, "effectiveFrom" | "effectiveTo">,
  now: Date = new Date()
): boolean =>
  subscription.effectiveTo !== null &&
  Date.parse(subscription.effectiveTo) <= now.getTime();

export const subscriptionState = (
  subscription: Subscription,
  now: Date = new Date()
): SubscriptionState => {
  if (isScheduled(subscription, now)) return "scheduled";
  if (isInForce(subscription, now)) return "inForce";
  return "superseded";
};

export const STATE_LABELS: Record<SubscriptionState, string> = {
  scheduled: "Scheduled",
  inForce: "In force",
  superseded: "Superseded",
};

export const inForceSubscription = (
  subscriptions: readonly Subscription[],
  now: Date = new Date()
): Subscription | null =>
  subscriptions.find((subscription) => isInForce(subscription, now)) ?? null;

export const scheduledSubscriptions = (
  subscriptions: readonly Subscription[],
  now: Date = new Date()
): Subscription[] =>
  subscriptions
    .filter((subscription) => isScheduled(subscription, now))
    .sort(
      (a, b) => Date.parse(a.effectiveFrom) - Date.parse(b.effectiveFrom)
    );

export const supersededSubscriptions = (
  subscriptions: readonly Subscription[],
  now: Date = new Date()
): Subscription[] =>
  subscriptions
    .filter((subscription) => isSuperseded(subscription, now))
    .sort(
      (a, b) => Date.parse(b.effectiveFrom) - Date.parse(a.effectiveFrom)
    );

export const NO_PLAN_HEADLINE = "This organisation is on no plan";

export const NO_PLAN_DETAIL =
  "It cannot be invoiced. The monthly billing job asks for the plan in effect, finds none, and skips the organisation silently — so this does not surface as a failure anywhere. Traffic is still being served and metered; it is simply not being charged for.";

export const isValidEffectiveFrom = (
  effectiveFrom: string,
  now: Date = new Date(),
  latestStart?: string | null
): boolean => {
  const parsed = Date.parse(effectiveFrom);
  if (Number.isNaN(parsed)) return false;
  if (parsed <= now.getTime()) return false;

  if (latestStart) {
    const latest = Date.parse(latestStart);
    if (!Number.isNaN(latest) && parsed <= latest) return false;
  }

  return true;
};

export const effectiveFromRefusal = (
  effectiveFrom: string,
  now: Date = new Date(),
  latestStart?: string | null
): string | null => {
  const parsed = Date.parse(effectiveFrom);
  if (Number.isNaN(parsed)) return "That is not a date.";

  if (parsed <= now.getTime()) {
    return "A subscription cannot be backdated. Pick a time in the future, or start it now — a month that has already been invoiced has to stay re-derivable from the plan the invoice names.";
  }

  if (latestStart) {
    const latest = Date.parse(latestStart);
    if (!Number.isNaN(latest) && parsed <= latest) {
      return "This organisation already has a stretch starting on or after that date. Cancel the scheduled change first, then schedule this one — the timeline is appended to, not written over.";
    }
  }

  return null;
};

export const BACKDATED_MESSAGE =
  "That start date is on or before the organisation's most recent stretch. Reload the timeline — a change may have been scheduled since this screen was opened — then cancel it or pick a later date.";

export const STARTS_NOW_HINT =
  "The current stretch closes at this instant and the new one opens. Whatever has been metered so far this month is invoiced at the old rate; everything after it, at the new one.";

export const STARTS_LATER_HINT =
  "The current stretch stays in force and closes at exactly that instant, when this one opens. Nothing changes until then, and the customer's own screen shows the change from now.";

export const canCancel = (
  subscription: Subscription,
  now: Date = new Date()
): boolean => isScheduled(subscription, now);

export const CANNOT_CANCEL_STARTED =
  "Only a change that has not started yet can be cancelled. A stretch already in force is a period this organisation was on that plan, and the invoices raised for those months were priced from it. To move them off it, subscribe them to something else.";

export const CANCEL_TOO_LATE_MESSAGE =
  "That change has already taken effect, so it can no longer be cancelled. Subscribe the organisation to another plan instead — the stretch it is on now stays on the record.";

export const CANCEL_REOPENS_PREDECESSOR =
  "The stretch this was going to supersede goes back to being open-ended, so the organisation stays on what it is on now.";

const prosaicDate = (value: string): string => {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;

  return new Intl.DateTimeFormat(DOCUMENT_LOCALE, {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(parsed);
};

export const timelineSentence = (
  subscription: Subscription,
  successor?: Subscription | null
): string => {
  const plan = versionLabel(
    subscription.planName,
    subscription.planVersion?.versionNumber ?? null
  );

  const opening = `${plan} from ${prosaicDate(subscription.effectiveFrom)}`;

  if (subscription.effectiveTo === null) return `${opening}, ongoing`;

  const until = `${opening}, until ${prosaicDate(subscription.effectiveTo)}`;

  if (!successor) return until;

  const next = versionLabel(
    successor.planName,
    successor.planVersion?.versionNumber ?? null
  );

  return `${until}, when ${next} starts`;
};

export const successorOf = (
  subscription: Subscription,
  subscriptions: readonly Subscription[]
): Subscription | null => {
  if (subscription.effectiveTo === null) return null;

  const closesAt = Date.parse(subscription.effectiveTo);

  return (
    subscriptions.find(
      (candidate) =>
        candidate.subscriptionId !== subscription.subscriptionId &&
        Date.parse(candidate.effectiveFrom) === closesAt
    ) ?? null
  );
};

export const invoiceSnapshotSentence = (invoice: {
  planVersionId: string | null;
  planVersionNumber: number | null;
  costPerRequest: number;
  planCurrency: CurrencyCode;
}): string =>
  invoice.planVersionId === null
    ? `Priced at ${rateSentence(invoice.costPerRequest, invoice.planCurrency)}. This invoice predates plans — the rate was taken from the organisation record rather than from a plan version, so there is no version to quote.`
    : `Priced at ${rateSentence(invoice.costPerRequest, invoice.planCurrency)}, from plan version ${invoice.planVersionNumber ?? "?"}.`;

export const invoiceVersionLabel = (
  invoice: { planVersionId: string | null; planVersionNumber: number | null },
  planName: string | null = null
): string =>
  invoice.planVersionId === null
    ? "Predates plans"
    : versionLabel(planName, invoice.planVersionNumber);

export const versionsNewestFirst = (
  versions: readonly PlanVersion[]
): PlanVersion[] =>
  [...versions].sort((a, b) => b.versionNumber - a.versionNumber);

export const isCurrentVersion = (plan: Plan, version: PlanVersion): boolean =>
  version.versionNumber === plan.currentVersionNumber;
