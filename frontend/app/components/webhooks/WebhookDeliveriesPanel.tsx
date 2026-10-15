"use client";

import React, { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { History, Repeat, RotateCw, TriangleAlert } from "lucide-react";

import {
  WebhookConfiguration,
  WebhookConfigurationResource,
  WebhookDeliveryResource,
  WebhookDeliveryResponse,
  WebhookReplayBatch,
} from "@/types";
import { DateRangeField } from "@/components/DateRangePicker";
import { DateTimePicker } from "@/components/DateTimePicker";
import { LocalTime } from "@/components/LocalTime";
import { PageHeader } from "@/components/PageHeader";
import Pagination from "@/components/Pagination";
import { PayloadView } from "@/components/PayloadView";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { useWebhookCatalogue } from "@/lib/useWebhookCatalogue";
import { cn } from "@/lib/utils";
import {
  DELIVERY_OUTCOME_LABELS,
  DELIVERY_OUTCOME_SENTENCES,
  INCLUDE_DELIVERED_EXPLAINS,
  NO_ANSWER_MEANS,
  REPLAY_SENDS_AGAIN,
  REPLAY_WINDOW_EXPLAINS,
  deliveryOutcome,
  deliveryOutcomeTone,
  formatLatency,
  parsePayload,
  replayBatchSentence,
  replayWindowRefusal,
  responseStatusLabel,
  responseStatusTone,
} from "@/lib/webhooks";
import { useTranslation } from "@/context/LocalizationContext";

const ANY = "any";

export function WebhookDeliveriesPanel({
  organizationSegment,
  canWrite,
  initialWebhookConfigurationId,
  configurationHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationSegment: string;
  canWrite: boolean;
  initialWebhookConfigurationId?: string;
  configurationHref?: (webhookConfigurationId: string) => string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const { catalogue } = useWebhookCatalogue();

  const [deliveries, setDeliveries] = useState<WebhookDeliveryResponse[]>([]);
  const [configurations, setConfigurations] = useState<WebhookConfiguration[]>(
    []
  );
  const [loading, setLoading] = useState<boolean>(true);

  const [pageNumber, setPageNumber] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(25);
  const [totalPages, setTotalPages] = useState<number>(1);

  const [configurationFilter, setConfigurationFilter] = useState<string>(
    initialWebhookConfigurationId ?? ANY
  );
  const [eventFilter, setEventFilter] = useState<string>(ANY);
  const [deliveredFilter, setDeliveredFilter] = useState<string>(ANY);
  const [fromDate, setFromDate] = useState<string>("");
  const [toDate, setToDate] = useState<string>("");

  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [replaying, setReplaying] = useState<string | null>(null);
  const [windowOpen, setWindowOpen] = useState<boolean>(false);

  // One key per row, and one for the window replay — the API binds a key to a
  // single method and path, and a key shared across rows would be answered with
  // a 409 rather than the protection it looks like.
  const replayIntent = useIdempotencyKey();

  const toggle = (outboxMessageId: string) =>
    setExpanded((previous) => {
      const next = new Set(previous);
      if (next.has(outboxMessageId)) next.delete(outboxMessageId);
      else next.add(outboxMessageId);
      return next;
    });

  const fetchDeliveries = useCallback(async () => {
    setLoading(true);

    try {
      const params = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (configurationFilter !== ANY) {
        params.set("WebhookConfigurationId", configurationFilter);
      }
      if (eventFilter !== ANY) params.set("Event", eventFilter);
      if (deliveredFilter !== ANY) params.set("IsDelivered", deliveredFilter);
      if (fromDate) params.set("FromDate", fromDate);
      if (toDate) params.set("ToDate", toDate);

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/webhook-deliveries?${params}`,
        { credentials: "include" }
      );

      if (response.ok) {
        const page = await unwrapPage<WebhookDeliveryResource>(response);
        setDeliveries(page.data.map((resource) => resource.webhookDelivery));
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("webhooks.failedLoadWebhookDeliveries"),
          onUnauthorized,
          toastId: "error-webhook-deliveries",
        });
      }
    } catch {
      toast.error(t("webhooks.failedLoadWebhookDeliveries"), {
        id: "error-webhook-deliveries",
      });
    } finally {
      setLoading(false);
    }
  }, [
    organizationSegment,
    pageNumber,
    pageSize,
    configurationFilter,
    eventFilter,
    deliveredFilter,
    fromDate,
    toDate,
    onUnauthorized,
  ]);

  const fetchConfigurations = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/webhook-configurations?PageNumber=1&PageSize=100`,
        { credentials: "include" }
      );

      if (!response.ok) return;

      const page = await unwrapPage<WebhookConfigurationResource>(response);
      setConfigurations(
        page.data.map((resource) => resource.webhookConfiguration)
      );
    } catch {
    }
  }, [organizationSegment]);

  useEffect(() => {
    fetchDeliveries();
  }, [fetchDeliveries]);

  useEffect(() => {
    fetchConfigurations();
  }, [fetchConfigurations]);

  const endpointOf = (webhookConfigurationId: string | null): string | null => {
    if (webhookConfigurationId === null) return null;

    return (
      configurations.find(
        (configuration) =>
          configuration.webhookConfigurationId === webhookConfigurationId
      )?.endpointUrl ?? null
    );
  };

  const replayOne = async (delivery: WebhookDeliveryResponse) => {
    setReplaying(delivery.outboxMessageId);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/webhook-deliveries/${delivery.outboxMessageId}/replay`,
        { method: "POST", credentials: "include" },
        { idempotencyKey: replayIntent.current(delivery.outboxMessageId) }
      );

      if (response.ok) {
        const { webhookDelivery: replay } =
          await unwrap<WebhookDeliveryResource>(response);
        replayIntent.completed(delivery.outboxMessageId);

        toast.success(t("webhooks.queuedAgain"), {
          description: t(
            "webhooks.queuedForReplayDescription",
            delivery.event ?? t("webhooks.theMessage"),
            replay.destination,
            replay.outboxMessageId
          ),
        });

        fetchDeliveries();
      } else {
        await reportApiError(response, {
          fallback: t("webhooks.failedQueueDeliveryAgain"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("webhooks.failedQueueDeliveryAgain"));
    } finally {
      setReplaying(null);
    }
  };

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("webhooks.webhooks")}
          title={t("webhooks.webhookDeliveries")}
          description={t("webhooks.everyMessageThisOrganizationSent")}
        >
          {canWrite && (
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={() => setWindowOpen(true)}
            >
              <History className="size-4" />
              {t("webhooks.replayAWindow")}
            </Button>
          )}
        </PageHeader>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <Select
          value={configurationFilter}
          onValueChange={(value) => {
            setConfigurationFilter(value);
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-64">
            <SelectValue placeholder={t("webhooks.anyEndpoint")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ANY}>{t("webhooks.anyEndpoint")}</SelectItem>
            {configurations.map((configuration) => (
              <SelectItem
                key={configuration.webhookConfigurationId}
                value={configuration.webhookConfigurationId}
              >
                {configuration.endpointUrl}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={eventFilter}
          onValueChange={(value) => {
            setEventFilter(value);
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-56">
            <SelectValue placeholder={t("webhooks.anyEvent")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ANY}>{t("webhooks.anyEvent")}</SelectItem>
            {(catalogue?.events ?? []).map((definition) => (
              <SelectItem key={definition.event} value={definition.event}>
                {definition.event}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={deliveredFilter}
          onValueChange={(value) => {
            setDeliveredFilter(value);
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-44">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ANY}>{t("webhooks.anyOutcome")}</SelectItem>
            <SelectItem value="true">{t("webhooks.delivered")}</SelectItem>
            <SelectItem value="false">{t("webhooks.notDelivered")}</SelectItem>
          </SelectContent>
        </Select>

        <DateRangeField
          from={fromDate}
          to={toDate}
          onChange={(from, to) => {
            setFromDate(from);
            setToDate(to);
            setPageNumber(1);
          }}
          aria-label={t("webhooks.filterDeliveriesByDate")}
        />

        <div className="ml-auto flex items-center gap-3">
          <Button
            variant="ghost"
            size="sm"
            className="gap-1.5 text-slate-500"
            onClick={() => fetchDeliveries()}
          >
            <RotateCw className="size-4" />
            {t("webhooks.refresh")}
          </Button>
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {embedded && canWrite && (
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={() => setWindowOpen(true)}
            >
              <History className="size-4" />
              {t("webhooks.replayAWindow")}
            </Button>
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("webhooks.event")}</TableHead>
              <TableHead>{t("webhooks.destination")}</TableHead>
              <TableHead className="w-28">{t("webhooks.outcome")}</TableHead>
              <TableHead className="w-28">{t("webhooks.response")}</TableHead>
              <TableHead className="w-24">{t("webhooks.attempts")}</TableHead>
              <TableHead className="w-40">{t("webhooks.lastAttempt")}</TableHead>
              <TableHead className="w-40">{t("common.details")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 7 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : deliveries.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={7}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("webhooks.noDeliveriesFound")}
                </TableCell>
              </TableRow>
            ) : (
              deliveries.map((delivery) => {
                const outcome = deliveryOutcome(delivery);
                const endpoint = endpointOf(delivery.webhookConfigurationId);
                const isOpen = expanded.has(delivery.outboxMessageId);

                return (
                  <React.Fragment key={delivery.outboxMessageId}>
                    <TableRow className="hover:bg-slate-50/50">
                      <TableCell>
                        <div className="flex flex-col gap-0.5">
                          <code className="font-mono text-xs text-slate-900">
                            {delivery.event ?? "—"}
                          </code>
                          {delivery.replayOfOutboxMessageId !== null && (
                            <span className="font-mono text-[11px] text-slate-400">
                              replay
                            </span>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="max-w-xs">
                        <div className="flex flex-col gap-0.5">
                          <span className="truncate text-sm text-slate-700">
                            {delivery.destination}
                          </span>
                          {endpoint !== null &&
                            endpoint !== delivery.destination && (
                              <span className="truncate font-mono text-[11px] text-slate-400">
                                endpoint now: {endpoint}
                              </span>
                            )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <ToneBadge tone={deliveryOutcomeTone(outcome)}>
                          {DELIVERY_OUTCOME_LABELS[outcome]}
                        </ToneBadge>
                      </TableCell>
                      <TableCell>
                        {outcome === "suppressed" ? (
                          <span className="font-mono text-xs text-slate-400">
                            —
                          </span>
                        ) : (
                          <ToneBadge tone={responseStatusTone(delivery)}>
                            {responseStatusLabel(delivery)}
                          </ToneBadge>
                        )}
                      </TableCell>
                      <TableCell className="font-mono text-sm text-slate-700">
                        {delivery.attempts}
                      </TableCell>
                      <TableCell className="text-sm text-slate-600">
                        {delivery.lastAttemptedAt === null ? (
                          <span className="font-mono text-xs text-slate-400">
                            {t("webhooks.notYet")}
                          </span>
                        ) : (
                          <LocalTime value={delivery.lastAttemptedAt} />
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
                            onClick={() => toggle(delivery.outboxMessageId)}
                          >
                            {isOpen ? "Hide" : "Show"}
                          </Button>
                          {canWrite && (
                            <Button
                              variant="outline"
                              size="sm"
                              className="gap-1.5"
                              title={REPLAY_SENDS_AGAIN}
                              disabled={
                                replaying === delivery.outboxMessageId
                              }
                              onClick={() => replayOne(delivery)}
                            >
                              <Repeat className="size-4" />
                              {replaying === delivery.outboxMessageId
                                ? "Queuing..."
                                : "Replay"}
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>

                    {isOpen && (
                      <TableRow className="bg-slate-50 hover:bg-slate-50">
                        <TableCell colSpan={7} className="py-3">
                          <DeliveryDetail
                            delivery={delivery}
                            configurationHref={
                              delivery.webhookConfigurationId !== null
                                ? configurationHref
                                : undefined
                            }
                            onOpenConfiguration={(href) => router.push(href)}
                          />
                        </TableCell>
                      </TableRow>
                    )}
                  </React.Fragment>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {deliveries.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}

      <ReplayWindowDialog
        open={windowOpen}
        onOpenChange={setWindowOpen}
        organizationSegment={organizationSegment}
        configurations={configurations}
        events={(catalogue?.events ?? []).map((definition) => definition.event)}
        defaultWebhookConfigurationId={
          configurationFilter === ANY ? undefined : configurationFilter
        }
        onQueued={() => fetchDeliveries()}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}

function DeliveryDetail({
  delivery,
  configurationHref,
  onOpenConfiguration,
}: {
  delivery: WebhookDeliveryResponse;
  configurationHref?: (webhookConfigurationId: string) => string;
  onOpenConfiguration: (href: string) => void;
}) {
  const t = useTranslation();
  const outcome = deliveryOutcome(delivery);
  const parsed = parsePayload(delivery.payload);
  const noAnswer =
    delivery.responseStatusCode === null && outcome !== "suppressed";

  return (
    <div className="space-y-3">
      <p className="max-w-prose text-sm leading-relaxed text-slate-600">
        {DELIVERY_OUTCOME_SENTENCES[outcome]}
      </p>

      {delivery.replayOfOutboxMessageId !== null && (
        <p className="max-w-prose text-xs leading-relaxed text-slate-500">
          This is a replay of{" "}
          <code className="font-mono text-slate-600">
            {delivery.replayOfOutboxMessageId}
          </code>
          . The original row is untouched, which is why the same event appears
          twice in this log.
        </p>
      )}

      <div className="grid grid-cols-2 gap-x-8 gap-y-3 sm:grid-cols-4">
        <Field label={t("webhooks.payloadVersion")} value={delivery.payloadVersion} />
        <Field label={t("webhooks.latency")} value={formatLatency(delivery.latencyMilliseconds)} />
        <Field
          label={t("webhooks.deliveredAt")}
          value={delivery.deliveredAt}
          asTime
        />
        <Field label={t("webhooks.suppressedAt")} value={delivery.suppressedAt} asTime />
        <Field label={t("webhooks.queuedAt")} value={delivery.createdAt} asTime />
        <Field label={t("webhooks.mode")} value={delivery.mode} />
        <Field label={t("webhooks.messageId")} value={delivery.outboxMessageId} mono />
        {delivery.webhookConfigurationId !== null && (
          <div className="space-y-0.5">
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {t("webhooks.endpoint")}
            </p>
            {configurationHref ? (
              <button
                type="button"
                className="text-left font-mono text-xs text-brand-800 underline-offset-2 hover:underline"
                onClick={() =>
                  onOpenConfiguration(
                    configurationHref(delivery.webhookConfigurationId as string)
                  )
                }
              >
                {delivery.webhookConfigurationId}
              </button>
            ) : (
              <p className="font-mono text-xs text-slate-700">
                {delivery.webhookConfigurationId}
              </p>
            )}
          </div>
        )}
      </div>

      <div className="space-y-1.5">
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            {t("webhooks.response")}
          </p>
          {outcome !== "suppressed" && (
            <ToneBadge tone={responseStatusTone(delivery)}>
              {responseStatusLabel(delivery)}
            </ToneBadge>
          )}
        </div>
        {noAnswer && (
          <div className="flex items-start gap-2.5 rounded-lg border border-critical-200 bg-critical-50 px-3 py-2.5">
            <TriangleAlert
              aria-hidden
              className="mt-0.5 size-4 flex-shrink-0 text-critical-700"
            />
            <p className="text-sm leading-relaxed text-critical-700">
              {NO_ANSWER_MEANS}
            </p>
          </div>
        )}
        {delivery.responseBody ? (
          <pre className="max-h-48 overflow-auto whitespace-pre-wrap break-words rounded-lg border border-slate-200 bg-card p-3 font-mono text-xs text-slate-700">
            {delivery.responseBody}
          </pre>
        ) : (
          <p className="font-mono text-xs text-slate-400">
            {outcome === "suppressed"
              ? "Nothing was sent, so nothing answered."
              : "The endpoint answered with an empty body."}
          </p>
        )}
      </div>

      <div className="space-y-1.5">
        <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
          {t("webhooks.requestPayload")}
        </p>
        {parsed === null ? (
          <pre className="max-h-96 overflow-auto whitespace-pre-wrap break-words rounded-lg border border-slate-200 bg-card p-3 font-mono text-xs text-slate-700">
            {delivery.payload}
          </pre>
        ) : (
          <PayloadView
            payload={parsed}
            label={`${delivery.event ?? "payload"} · as sent`}
            defaultView="json"
          />
        )}
      </div>
    </div>
  );
}

function Field({
  label,
  value,
  asTime = false,
  mono = false,
}: {
  label: string;
  value: string | number | null;
  asTime?: boolean;
  mono?: boolean;
}) {
  return (
    <div className="space-y-0.5">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      {value === null ? (
        <p className="font-mono text-xs text-slate-400">—</p>
      ) : asTime ? (
        <LocalTime
          value={String(value)}
          className="font-mono text-xs text-slate-700"
        />
      ) : (
        <p
          className={cn(
            "text-xs text-slate-700",
            mono ? "font-mono break-all" : "font-mono"
          )}
        >
          {value}
        </p>
      )}
    </div>
  );
}

function ReplayWindowDialog({
  open,
  onOpenChange,
  organizationSegment,
  configurations,
  events,
  defaultWebhookConfigurationId,
  onQueued,
  onUnauthorized,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  organizationSegment: string;
  configurations: WebhookConfiguration[];
  events: string[];
  defaultWebhookConfigurationId?: string;
  onQueued: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [from, setFrom] = useState<string>("");
  const [to, setTo] = useState<string>("");
  const [configurationId, setConfigurationId] = useState<string>(
    defaultWebhookConfigurationId ?? ANY
  );
  const [event, setEvent] = useState<string>(ANY);
  const [includeDelivered, setIncludeDelivered] = useState<boolean>(false);
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [batch, setBatch] = useState<WebhookReplayBatch | null>(null);

  const intent = useIdempotencyKey();
  const refusal = replayWindowRefusal({ from, to });

  const submit = async () => {
    if (refusal !== null) {
      toast.error(refusal);
      return;
    }

    setSubmitting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/webhook-deliveries/replay`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            from: new Date(from).toISOString(),
            to: new Date(to).toISOString(),
            ...(configurationId === ANY
              ? {}
              : { webhookConfigurationId: configurationId }),
            ...(event === ANY ? {} : { event }),
            includeDelivered,
          }),
        },
        { idempotencyKey: intent.current() }
      );

      if (response.ok) {
        const result = await unwrap<WebhookReplayBatch>(response);
        intent.completed();
        setBatch(result);
        onQueued();

        if (!result.hasMore) {
          toast.success(t("webhooks.replayQueued"), {
            description: replayBatchSentence(result),
          });
        }
      } else {
        await reportApiError(response, {
          fallback: t("webhooks.failedQueueReplay"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("webhooks.failedQueueReplay"));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        onOpenChange(next);
        if (!next) setBatch(null);
      }}
    >
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("webhooks.replayAWindow")}</DialogTitle>
        </DialogHeader>

        <p className="text-sm leading-relaxed text-slate-600">
          {REPLAY_WINDOW_EXPLAINS}
        </p>

        <div className="space-y-3">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                {t("common.from")}
              </label>
              <DateTimePicker
                value={from}
                onChange={setFrom}
                aria-label={t("webhooks.replayWindowStart")}
              />
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                To
              </label>
              <DateTimePicker
                value={to}
                onChange={setTo}
                aria-label={t("webhooks.replayWindowEnd")}
              />
            </div>
          </div>

          <Select value={configurationId} onValueChange={setConfigurationId}>
            <SelectTrigger>
              <SelectValue placeholder={t("webhooks.everyEndpoint")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY}>{t("webhooks.everyEndpoint")}</SelectItem>
              {configurations.map((configuration) => (
                <SelectItem
                  key={configuration.webhookConfigurationId}
                  value={configuration.webhookConfigurationId}
                >
                  {configuration.endpointUrl}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={event} onValueChange={setEvent}>
            <SelectTrigger>
              <SelectValue placeholder={t("webhooks.everyEvent")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY}>{t("webhooks.everyEvent")}</SelectItem>
              {events.map((name) => (
                <SelectItem key={name} value={name}>
                  {name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <label className="flex items-start gap-2.5">
            <input
              type="checkbox"
              checked={includeDelivered}
              onChange={(e) => setIncludeDelivered(e.target.checked)}
              className="mt-0.5 h-4 w-4 rounded border-slate-300 accent-brand-800"
            />
            <span className="space-y-0.5">
              <span className="block text-sm text-slate-700">
                {t("webhooks.includeDeliveriesThatAlreadySucceeded")}
              </span>
              <span className="block text-xs leading-relaxed text-slate-500">
                {INCLUDE_DELIVERED_EXPLAINS}
              </span>
            </span>
          </label>

          {refusal !== null && (from || to) && (
            <p className="text-xs text-attention-700">{refusal}</p>
          )}

          {batch !== null && (
            <div
              className={cn(
                "space-y-1.5 rounded-lg border px-3 py-2.5",
                batch.hasMore
                  ? "border-attention-200 bg-attention-50"
                  : "border-slate-200 bg-slate-50"
              )}
            >
              <p
                className={cn(
                  "text-sm leading-relaxed",
                  batch.hasMore ? "text-attention-700" : "text-slate-600"
                )}
              >
                {replayBatchSentence(batch)}
              </p>
              <p className="font-mono text-[11px] text-slate-500">
                <LocalTime value={batch.from} /> — <LocalTime value={batch.to} />
              </p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            {batch === null ? "Cancel" : "Close"}
          </Button>
          <Button onClick={submit} disabled={submitting || refusal !== null}>
            {submitting ? "Queuing..." : "Queue replay"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
