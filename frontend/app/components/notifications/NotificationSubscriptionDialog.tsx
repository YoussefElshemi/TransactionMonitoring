"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import {
  NOTIFICATION_CHANNELS,
  NotificationChannel,
  NotificationSubscription,
  RuleSeverity,
} from "@/types";
import {
  BREACH_SWEEP_NOTE,
  BREACH_TRIGGER_HELP,
  CHANNEL_HINT,
  CHANNEL_LABEL,
  DESTINATION_INPUT_TYPE,
  DESTINATION_LABEL,
  DESTINATION_PLACEHOLDER,
  MAX_DESTINATION,
  SEVERITY_TRIGGER_HELP,
  destinationProblem,
  submitProblem,
} from "@/lib/notifications";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

const EVERY_SEVERITY = "Every";

export interface SubscriptionDraft {
  channel: NotificationChannel;
  destination: string;
  minimumSeverity: RuleSeverity | null;
  notifyOnBreach: boolean;
  isEnabled: boolean;
}

export function NotificationSubscriptionDialog({
  open,
  onOpenChange,
  existing,
  submitting,
  onSubmit,
  webhookHref,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  existing: NotificationSubscription | null;
  submitting: boolean;
  onSubmit: (draft: SubscriptionDraft) => void;
  webhookHref: string;
}) {
  const t = useTranslation();
  const editing = existing !== null;

  const [draft, setDraft] = useState<SubscriptionDraft>({
    channel: "Email",
    destination: "",
    minimumSeverity: null,
    notifyOnBreach: true,
    isEnabled: true,
  });
  const [touchedDestination, setTouchedDestination] = useState(false);

  useEffect(() => {
    if (!open) return;

    setTouchedDestination(false);
    setDraft(
      existing
        ? {
            channel: (existing.channel as NotificationChannel) ?? "Email",
            destination: existing.destination,
            minimumSeverity: existing.minimumSeverity,
            notifyOnBreach: existing.notifyOnBreach,
            isEnabled: existing.isEnabled,
          }
        : {
            channel: "Email",
            destination: "",
            minimumSeverity: null,
            notifyOnBreach: true,
            isEnabled: true,
          }
    );
  }, [open, existing]);

  const destinationError = destinationProblem(draft.channel, draft.destination);
  const blocker = submitProblem(draft);

  /**
   * Whether "every severity" can still be chosen.
   *
   * A `PATCH` sends only what changed, and an omitted `minimumSeverity` means
   * *unchanged* — there is no way to express "clear it". So on a subscription
   * that already has a band, dropping back to every severity is not something
   * this form can do, and the option is disabled with the reason rather than
   * silently doing nothing on save.
   */
  const canClearSeverity = !editing || existing.minimumSeverity === null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {editing ? "Edit subscription" : "New subscription"}
          </DialogTitle>
          <DialogDescription>
            Who hears about what. This is for a person watching a desk — a
            machine integration wanting every event is a{" "}
            <Link
              href={webhookHref}
              className="text-brand-800 underline underline-offset-2"
            >
              webhook configuration
            </Link>{" "}
            instead.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-5">
          <div className="space-y-1.5">
            <label
              htmlFor="subscription-channel"
              className="font-mono text-xs uppercase tracking-widest text-slate-400"
            >
              {t("notifications.channel")}
            </label>
            <Select
              value={draft.channel}
              onValueChange={(value) =>
                setDraft((current) => ({
                  ...current,
                  channel: value as NotificationChannel,
                  destination:
                    value === current.channel ? current.destination : "",
                }))
              }
              disabled={editing}
            >
              <SelectTrigger id="subscription-channel">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {NOTIFICATION_CHANNELS.map((candidate) => (
                  <SelectItem key={candidate} value={candidate}>
                    {CHANNEL_LABEL[candidate]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-slate-500">
              {editing
                ? "The channel is fixed once a subscription exists. Point somewhere else by creating a new one."
                : CHANNEL_HINT[draft.channel]}
            </p>
          </div>

          <div className="space-y-1.5">
            <label
              htmlFor="subscription-destination"
              className="font-mono text-xs uppercase tracking-widest text-slate-400"
            >
              {DESTINATION_LABEL[draft.channel]}
            </label>
            <Input
              id="subscription-destination"
              type={DESTINATION_INPUT_TYPE[draft.channel]}
              value={draft.destination}
              maxLength={MAX_DESTINATION}
              placeholder={DESTINATION_PLACEHOLDER[draft.channel]}
              aria-invalid={touchedDestination && destinationError !== null}
              aria-describedby="subscription-destination-error"
              onBlur={() => setTouchedDestination(true)}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  destination: event.target.value,
                }))
              }
            />
            <p
              id="subscription-destination-error"
              className="min-h-4 text-xs text-critical-700"
            >
              {touchedDestination && destinationError}
            </p>
          </div>

          <div className="space-y-1.5">
            <label
              htmlFor="subscription-severity"
              className="font-mono text-xs uppercase tracking-widest text-slate-400"
            >
              {t("notifications.severityFloor")}
            </label>
            <Select
              value={draft.minimumSeverity ?? EVERY_SEVERITY}
              onValueChange={(value) =>
                setDraft((current) => ({
                  ...current,
                  minimumSeverity:
                    value === EVERY_SEVERITY ? null : (value as RuleSeverity),
                }))
              }
            >
              <SelectTrigger id="subscription-severity">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={EVERY_SEVERITY} disabled={!canClearSeverity}>
                  {t("notifications.everySeverity")}
                </SelectItem>
                {SEVERITY_FILTERS.map((candidate) => (
                  <SelectItem key={candidate} value={candidate}>
                    {candidate} and above
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-slate-500">
              {SEVERITY_TRIGGER_HELP}
            </p>
            {!canClearSeverity && (
              <p className="text-xs text-slate-400">
                Going back to every severity means creating a new subscription —
                an edit can move the floor but not remove it.
              </p>
            )}
          </div>

          <PolicySwitch
            label={t("notifications.notifyOnBreach")}
            checked={draft.notifyOnBreach}
            onChange={(next) =>
              setDraft((current) => ({ ...current, notifyOnBreach: next }))
            }
          >
            {BREACH_TRIGGER_HELP} {BREACH_SWEEP_NOTE}
          </PolicySwitch>

          <PolicySwitch
            label={t("common.enabled")}
            checked={draft.isEnabled}
            onChange={(next) =>
              setDraft((current) => ({ ...current, isEnabled: next }))
            }
          >
            Turning this off keeps the subscription and stops delivery. Nothing
            is queued while it is off — the messages are not held back, they are
            not sent.
          </PolicySwitch>
        </div>

        <DialogFooter className="flex-col items-stretch gap-2 sm:flex-row sm:items-center sm:justify-end">
          {blocker && (destinationError === null || touchedDestination) && (
            <p className="mr-auto max-w-xs text-left text-xs text-slate-500">
              {blocker}
            </p>
          )}
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            disabled={blocker !== null || submitting}
            onClick={() => onSubmit(draft)}
          >
            {submitting
              ? "Saving…"
              : editing
              ? "Save changes"
              : "Create subscription"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
