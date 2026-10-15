"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { BellOff, History, Pencil, Plus, Trash2 } from "lucide-react";

import {
  NOTIFICATION_CHANNELS,
  NotificationSubscription,
  NotificationSubscriptionPatch,
  NotificationSubscriptionResource,
} from "@/types";
import { organizationScopedPath, reportApiError, unwrapPage } from "@/lib/api";
import {
  BREACH_SWEEP_NOTE,
  CHANNEL_LABEL,
  SUBSCRIPTION_FIELD_LABELS,
  asksForSomething,
  severityTriggerLabel,
} from "@/lib/notifications";
import { LocalTime } from "@/components/LocalTime";
import Pagination from "@/components/Pagination";
import { PolicySwitch } from "@/components/PolicySwitch";
import { StatusBadge } from "@/components/StatusBadge";
import {
  NotificationSubscriptionDialog,
  SubscriptionDraft,
} from "@/components/notifications/NotificationSubscriptionDialog";
import { HistoryDialog, historyPath } from "@/components/history";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY = "All";
const ENABLED_ANY = "All";
const PAGE_SIZE = 10;

export function NotificationSubscriptionsPanel({
  organizationId,
  webhookHref,
  onUnauthorized,
}: {
  organizationId?: string;
  webhookHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const collectionPath = organizationScopedPath(
    "notification-subscriptions",
    organizationId
  );
  const subscriptionPath = (notificationSubscriptionId: string) =>
    `${collectionPath}/${notificationSubscriptionId}`;

  const [subscriptions, setSubscriptions] = useState<
    NotificationSubscriptionResource[]
  >([]);
  const [channel, setChannel] = useState(ANY);
  const [enabled, setEnabled] = useState(ENABLED_ANY);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<NotificationSubscription | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [toggling, setToggling] = useState<string | null>(null);
  const [historyOf, setHistoryOf] = useState<NotificationSubscription | null>(
    null
  );

  const fetchSubscriptions = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PageNumber: String(pageNumber),
      PageSize: String(PAGE_SIZE),
    });
    if (channel !== ANY) params.set("Channel", channel);
    if (enabled !== ENABLED_ANY) params.set("IsEnabled", enabled);

    try {
      const response = await apiFetch(
        `${collectionPath}?${params}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<NotificationSubscriptionResource>(
          response
        );
        setSubscriptions(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setSubscriptions([]);
        await reportApiError(response, {
          fallback: t("notifications.errorOccurredFetchingNotificationSubscriptions"),
          onUnauthorized,
          toastId: "error-fetching-subscriptions",
        });
      }
    } catch {
      setSubscriptions([]);
      toast.error(t("notifications.errorOccurredFetchingNotificationSubscriptions"), {
        id: "error-fetching-subscriptions",
      });
    } finally {
      setLoading(false);
    }
  }, [collectionPath, channel, enabled, pageNumber, onUnauthorized]);

  useEffect(() => {
    fetchSubscriptions();
  }, [fetchSubscriptions]);

  const handleSubmit = async (draft: SubscriptionDraft) => {
    setSubmitting(true);

    const target = editing;

    try {
      const response = await apiFetch(
        target
          ? subscriptionPath(target.notificationSubscriptionId)
          : collectionPath,
        {
          method: target ? "PATCH" : "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(
            target
              ? // Only what moved. An omitted field means unchanged, which is
                // also why `minimumSeverity: null` is never sent — it would
                // read as "leave it alone" rather than as "clear it", and the
                // dialog disables that choice for exactly this reason.
                ({
                  ...(draft.destination !== target.destination && {
                    destination: draft.destination.trim(),
                  }),
                  ...(draft.minimumSeverity !== null &&
                    draft.minimumSeverity !== target.minimumSeverity && {
                      minimumSeverity: draft.minimumSeverity,
                    }),
                  ...(draft.notifyOnBreach !== target.notifyOnBreach && {
                    notifyOnBreach: draft.notifyOnBreach,
                  }),
                  ...(draft.isEnabled !== target.isEnabled && {
                    isEnabled: draft.isEnabled,
                  }),
                } satisfies NotificationSubscriptionPatch)
              : {
                  channel: draft.channel,
                  destination: draft.destination.trim(),
                  minimumSeverity: draft.minimumSeverity,
                  notifyOnBreach: draft.notifyOnBreach,
                  isEnabled: draft.isEnabled,
                }
          ),
        }
      );

      if (response.ok) {
        setDialogOpen(false);
        setEditing(null);
        if (!target) setPageNumber(1);
        fetchSubscriptions();
        toast.success(target ? t("notifications.subscriptionUpdated") : t("notifications.subscriptionCreated"));
        return;
      }

      await reportApiError(response, {
        fallback: target
          ? t("notifications.subscriptionCouldNotUpdated")
          : t("notifications.subscriptionCouldNotCreated"),
        onUnauthorized,
        toastId: "error-saving-subscription",
        fieldLabels: SUBSCRIPTION_FIELD_LABELS,
      });
    } catch {
      toast.error(t("notifications.subscriptionCouldNotSaved"), {
        id: "error-saving-subscription",
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggle = async (
    subscription: NotificationSubscription,
    next: boolean
  ) => {
    if (next && !asksForSomething(subscription)) {
      toast.error(t("notifications.subscriptionAsksNothingSoEnabling"), {
        description:
          t("notifications.giveSeverityFloorTurnBreach"),
      });
      return;
    }

    setToggling(subscription.notificationSubscriptionId);
    try {
      const response = await apiFetch(
        subscriptionPath(subscription.notificationSubscriptionId),
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ isEnabled: next }),
        }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("notifications.subscriptionCouldNotChanged"),
          onUnauthorized,
          toastId: `toggle-${subscription.notificationSubscriptionId}`,
          fieldLabels: SUBSCRIPTION_FIELD_LABELS,
        });
        return;
      }

      setSubscriptions((current) =>
        current.map((resource) =>
          resource.notificationSubscription.notificationSubscriptionId ===
          subscription.notificationSubscriptionId
            ? {
                ...resource,
                notificationSubscription: {
                  ...resource.notificationSubscription,
                  isEnabled: next,
                },
              }
            : resource
        )
      );
    } catch {
      toast.error(t("notifications.subscriptionCouldNotChanged"), {
        id: `toggle-${subscription.notificationSubscriptionId}`,
      });
    } finally {
      setToggling(null);
    }
  };

  const handleDelete = async (subscription: NotificationSubscription) => {
    try {
      const response = await apiFetch(
        subscriptionPath(subscription.notificationSubscriptionId),
        { method: "DELETE", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("notifications.subscriptionCouldNotRemoved"),
          onUnauthorized,
          toastId: `delete-${subscription.notificationSubscriptionId}`,
        });
        return;
      }

      fetchSubscriptions();
      toast.success(t("notifications.noLongerNotifying", subscription.destination));
    } catch {
      toast.error(t("notifications.subscriptionCouldNotRemoved"), {
        id: `delete-${subscription.notificationSubscriptionId}`,
      });
    }
  };

  const filtered = channel !== ANY || enabled !== ENABLED_ANY;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="max-w-prose text-sm text-slate-500">
          Where this desk hears about its own work — a case opening at a
          severity somebody watches, or one going past its deadline. For a
          machine integration that wants every event, use a{" "}
          <Link
            href={webhookHref}
            className="text-brand-800 underline underline-offset-2"
          >
            webhook configuration
          </Link>
          .
        </p>
        <Button
          size="sm"
          className="gap-1.5"
          onClick={() => {
            setEditing(null);
            setDialogOpen(true);
          }}
        >
          <Plus className="size-4" />
          {t("notifications.newSubscription")}
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Select
            value={channel}
            onValueChange={(value) => {
              setChannel(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-40" aria-label={t("notifications.filterByChannel")}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY}>{t("notifications.anyChannel")}</SelectItem>
              {NOTIFICATION_CHANNELS.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {CHANNEL_LABEL[candidate]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={enabled}
            onValueChange={(value) => {
              setEnabled(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-40" aria-label={t("notifications.filterByEnabled")}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ENABLED_ANY}>{t("notifications.enabledAndNot")}</SelectItem>
              <SelectItem value="true">{t("notifications.enabledOnly")}</SelectItem>
              <SelectItem value="false">{t("notifications.switchedOffOnly")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
        {!loading && totalCount > 0 && (
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {totalCount} {totalCount === 1 ? "subscription" : "subscriptions"}
          </p>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-28">{t("notifications.channel")}</TableHead>
              <TableHead>{t("notifications.destination")}</TableHead>
              <TableHead className="w-64">{t("notifications.triggers")}</TableHead>
              <TableHead className="w-40">{t("common.enabled")}</TableHead>
              <TableHead className="w-32 text-right">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={5}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ))
            ) : subscriptions.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-12 text-center">
                  <BellOff className="mx-auto size-5 text-slate-300" />
                  <p className="mt-2 text-sm text-slate-500">
                    {filtered
                      ? "No subscription matches that"
                      : "Nobody is being notified"}
                  </p>
                  {!filtered && (
                    <p className="mx-auto mt-1 max-w-md px-4 text-xs text-slate-400">
                      Cases still open and still breach on their own — this only
                      decides who finds out without looking. {BREACH_SWEEP_NOTE}
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              subscriptions.map(({ notificationSubscription: subscription }) => {
                const silent = !asksForSomething(subscription);

                return (
                  <TableRow
                    key={subscription.notificationSubscriptionId}
                    className="align-top hover:bg-slate-50/50"
                  >
                    <TableCell>
                      <span className="font-mono text-xs text-slate-600">
                        {CHANNEL_LABEL[
                          subscription.channel as keyof typeof CHANNEL_LABEL
                        ] ?? subscription.channel}
                      </span>
                    </TableCell>
                    <TableCell className="max-w-xs">
                      <span className="break-all text-sm text-slate-900">
                        {subscription.destination}
                      </span>
                      <p className="mt-0.5 text-[11px] text-slate-400">
                        added <LocalTime value={subscription.createdAt} />
                      </p>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap items-center gap-1.5">
                        <StatusBadge
                          status="Assigned"
                          label={severityTriggerLabel(
                            subscription.minimumSeverity
                          )}
                        />
                        {subscription.notifyOnBreach && (
                          <StatusBadge status="Undeployed" label={t("notifications.onBreach")} />
                        )}
                      </div>
                      {silent && (
                        <p className="mt-1 text-[11px] text-critical-700">
                          {t("notifications.asksForNothingSoIt")}
                        </p>
                      )}
                      {subscription.notifyOnBreach &&
                        subscription.minimumSeverity && (
                          <p className="mt-1 text-[11px] text-slate-400">
                            Breaches ignore the floor — a case of any severity
                            notifies when it runs late.
                          </p>
                        )}
                    </TableCell>
                    <TableCell>
                      <PolicySwitch
                        label={subscription.isEnabled ? "On" : "Off"}
                        checked={subscription.isEnabled}
                        disabled={
                          toggling === subscription.notificationSubscriptionId
                        }
                        onChange={(next) => handleToggle(subscription, next)}
                      >
                        <span className="text-[11px]">
                          {subscription.isEnabled
                            ? "Delivering."
                            : "Kept, not delivering."}
                        </span>
                      </PolicySwitch>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-400 hover:text-slate-700"
                          title={t("common.edit")}
                          aria-label={t("common.edit")}
                          onClick={() => {
                            setEditing(subscription);
                            setDialogOpen(true);
                          }}
                        >
                          <Pencil className="size-4" />
                          <span className="sr-only">
                            Edit the subscription for {subscription.destination}
                          </span>
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-400 hover:text-slate-700"
                          title={t("notifications.viewHistory")}
                          aria-label={t("notifications.viewHistory")}
                          onClick={() => setHistoryOf(subscription)}
                        >
                          <History className="size-4" />
                          <span className="sr-only">
                            History of the subscription for{" "}
                            {subscription.destination}
                          </span>
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          title={t("common.delete")}
                          aria-label={t("common.delete")}
                          onClick={() => handleDelete(subscription)}
                        >
                          <Trash2 className="size-4" />
                          <span className="sr-only">
                            Delete the subscription for{" "}
                            {subscription.destination}
                          </span>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {subscriptions.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}

      <NotificationSubscriptionDialog
        open={dialogOpen}
        onOpenChange={(open) => {
          setDialogOpen(open);
          if (!open) setEditing(null);
        }}
        existing={editing}
        submitting={submitting}
        onSubmit={handleSubmit}
        webhookHref={webhookHref}
      />

      <HistoryDialog
        open={historyOf !== null}
        onOpenChange={(open) => !open && setHistoryOf(null)}
        title={`this subscription${
          historyOf ? ` for ${historyOf.destination}` : ""
        }`}
        entityType="NotificationSubscription"
        path={historyPath.NotificationSubscription(
          historyOf?.notificationSubscriptionId ?? ""
        )}
        entityId={historyOf?.notificationSubscriptionId}
        onUnauthorized={onUnauthorized}
        scopeNote="What was asked for, and by whom. Deliveries themselves are the engine's work and are not recorded here — there are far too many of them, and none is anybody's decision."
      />
    </div>
  );
}
