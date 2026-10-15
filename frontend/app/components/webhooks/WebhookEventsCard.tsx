"use client";

import { Info, TriangleAlert } from "lucide-react";

import type { WebhookEventDefinition } from "@/types";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ToneBadge } from "@/components/status/StatusChips";
import { cn } from "@/lib/utils";
import {
  EVERY_EVENT,
  groupEventsByCategory,
  notSubscribableReason,
  subscribableEvents,
  wideningWarning,
} from "@/lib/webhooks";
import {
  invalidateWebhookCatalogue,
  useWebhookCatalogue,
} from "@/lib/useWebhookCatalogue";
import { useTranslation } from "@/context/LocalizationContext";

export function WebhookEventsCard({
  selected,
  original,
  onChange,
  disabled = false,
  className,
}: {
  selected: string[];
  original?: string[];
  onChange?: (events: string[]) => void;
  disabled?: boolean;
  className?: string;
}) {
  const t = useTranslation();
  const { status, catalogue } = useWebhookCatalogue();
  const editable = Boolean(onChange) && !disabled;

  const events = catalogue?.events ?? [];
  const groups = groupEventsByCategory(events);
  const selectable = subscribableEvents(events);
  const everything = selected.length === 0;

  const toggle = (event: string) => {
    if (!onChange) return;

    onChange(
      selected.includes(event)
        ? selected.filter((name) => name !== event)
        : [...selected, event]
    );
  };

  const widening =
    original === undefined ? null : wideningWarning(original, selected);

  if (status === "loading") {
    return (
      <Card className={className}>
        <CardContent className="space-y-3 pt-6">
          <Skeleton className="h-4 w-64" />
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-4 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  if (status === "unavailable" || catalogue === null) {
    return (
      <Card className={className}>
        <CardContent className="space-y-3 pt-6">
          <h2 className="font-mono text-sm font-medium text-slate-900">
            {t("webhooks.whatThisEndpointReceives")}
          </h2>
          <p className="max-w-prose text-sm text-slate-500">
            The event catalogue could not be read, so the list of events is not
            available here. This endpoint&apos;s subscription is unchanged —{" "}
            {everything
              ? "it receives every event."
              : `it receives ${selected.length} named event${
                  selected.length === 1 ? "" : "s"
                }.`}
          </p>
          {!everything && (
            <div className="flex flex-wrap gap-1.5">
              {selected.map((event) => (
                <code
                  key={event}
                  className="rounded-md border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-mono text-xs text-slate-700"
                >
                  {event}
                </code>
              ))}
            </div>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={() => invalidateWebhookCatalogue()}
          >
            {t("common.retry")}
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={className}>
      <CardContent className="space-y-5 pt-6">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="font-mono text-sm font-medium text-slate-900">
              {t("webhooks.whatThisEndpointReceives")}
            </h2>
            <ToneBadge tone={everything ? "inflight" : "settled"}>
              {everything
                ? "All events"
                : `${selected.length} of ${selectable.length}`}
            </ToneBadge>
          </div>

          <div
            className={cn(
              "flex items-start gap-2.5 rounded-lg border px-3 py-2.5",
              everything
                ? "border-inflight-200 bg-inflight-50"
                : "border-slate-200 bg-slate-50"
            )}
          >
            <Info
              aria-hidden
              className={cn(
                "mt-0.5 size-4 flex-shrink-0",
                everything ? "text-inflight-700" : "text-slate-400"
              )}
            />
            <p
              className={cn(
                "text-sm leading-relaxed",
                everything ? "text-inflight-700" : "text-slate-600"
              )}
            >
              {everything
                ? EVERY_EVENT
                : "This endpoint receives only the events ticked below. An endpoint that only cares about some of them still has to branch on the event name it receives — a delivery names its own event."}
            </p>
          </div>

          {widening && (
            <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
              <TriangleAlert
                aria-hidden
                className="mt-0.5 size-4 flex-shrink-0 text-attention-700"
              />
              <p className="text-sm text-attention-700">{widening}</p>
            </div>
          )}

          {editable && (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => onChange?.([])}
                disabled={everything}
              >
                {t("webhooks.receiveEverything")}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() =>
                  onChange?.(selectable.map((definition) => definition.event))
                }
                disabled={
                  selected.length === selectable.length && selectable.length > 0
                }
                title={t("webhooks.tickEveryEventPublishedToday")}
              >
                {t("webhooks.pinTodayAposSList")}
              </Button>
            </div>
          )}
        </div>

        {groups.map((group) => (
          <div key={group.category}>
            <p className="mb-1 font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {group.label}
            </p>
            {group.events.map((definition) => (
              <EventRow
                key={definition.event}
                definition={definition}
                checked={everything || selected.includes(definition.event)}
                impliedByEverything={everything}
                currentPayloadVersion={catalogue.currentPayloadVersion}
                onToggle={editable ? () => toggle(definition.event) : undefined}
              />
            ))}
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

function EventRow({
  definition,
  checked,
  impliedByEverything,
  currentPayloadVersion,
  onToggle,
}: {
  definition: WebhookEventDefinition;
  checked: boolean;
  impliedByEverything: boolean;
  currentPayloadVersion: number;
  onToggle?: () => void;
}) {
  const t = useTranslation();
  const refusal = notSubscribableReason(definition);
  const selectable = refusal === null && onToggle !== undefined;

  const body = (
    <>
      <div className="flex w-full min-w-0 items-start gap-2 sm:w-64 sm:flex-shrink-0">
        {onToggle !== undefined && (
          <input
            type="checkbox"
            checked={refusal === null && checked}
            disabled={!selectable}
            onChange={() => onToggle?.()}
            aria-label={definition.event}
            className="h-4 w-4 flex-shrink-0 rounded border-slate-300 accent-brand-800 disabled:opacity-40"
          />
        )}
        <code
          className={cn(
            // `break-all` rather than `break-words`: an event name is one
            // unbroken token — no spaces, and neither the dots nor the
            // underscores are break opportunities — so anything gentler leaves
            // `suspicious_activity_report.acknowledged` overflowing its column.
            "min-w-0 break-all font-mono text-xs leading-4",
            refusal === null ? "text-slate-900" : "text-slate-400"
          )}
        >
          {definition.event}
        </code>
      </div>
      <div className="min-w-0 space-y-1">
        <p className="text-sm text-slate-600">{definition.description}</p>
        {refusal !== null && (
          <p className="text-xs leading-relaxed text-slate-500">{refusal}</p>
        )}
        {impliedByEverything && refusal === null && (
          <p className="text-xs text-slate-400">
            {t("webhooks.arrivingBecauseThisEndpointAsks")}
          </p>
        )}
        {definition.introducedInVersion !== currentPayloadVersion && (
          <p className="font-mono text-[11px] text-slate-400">
            added in payload v{definition.introducedInVersion}
          </p>
        )}
      </div>
    </>
  );

  const rowClasses =
    "flex flex-col gap-1 border-t border-slate-100 py-2.5 first:border-t-0 sm:flex-row sm:gap-4";

  return selectable ? (
    <label className={cn(rowClasses, "cursor-pointer")}>{body}</label>
  ) : (
    <div className={rowClasses}>{body}</div>
  );
}
