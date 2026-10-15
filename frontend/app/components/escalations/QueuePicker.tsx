"use client";

import { useCallback, useEffect, useState } from "react";

import { Queue, QueueResource } from "@/types";
import { API_BASE, unwrapPage } from "@/lib/api";
import { usePermissions } from "@/context/PermissionsContext";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectSeparator,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export const QUEUE_NONE = "None";

const fetchQueuePage = async (
  organizationId: string
): Promise<Queue[] | null> => {
  try {
    const response = await apiFetch(
      `${API_BASE}/organizations/${organizationId}/queues?PageNumber=1&PageSize=100`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      }
    );

    if (!response.ok) return null;

    const page = await unwrapPage<QueueResource>(response);
    return page.data.map(({ queue }) => queue);
  } catch {
    return null;
  }
};

export function QueuePicker({
  organizationId,
  value,
  onChange,
  disabled = false,
  className = "max-w-xs",
}: {
  organizationId: string;
  value: string | null;
  onChange: (queueId: string | null) => void;
  disabled?: boolean;
  className?: string;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();
  const canListQueues = holds("queues:read");

  const [queues, setQueues] = useState<Queue[] | null>(null);

  const fetchQueues = useCallback(async () => {
    if (!canListQueues) {
      setQueues(null);
      return;
    }

    setQueues(await fetchQueuePage(organizationId));
  }, [organizationId, canListQueues]);

  useEffect(() => {
    if (resolved) fetchQueues();
  }, [resolved, fetchQueues]);

  const listed = queues !== null;
  const chosenIsListed =
    value === null || (queues ?? []).some((queue) => queue.queueId === value);

  if (listed && chosenIsListed) {
    return (
      <Select
        value={value ?? QUEUE_NONE}
        onValueChange={(next) => onChange(next === QUEUE_NONE ? null : next)}
        disabled={disabled}
      >
        <SelectTrigger className={className}>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={QUEUE_NONE}>{t("escalations.leaveItWhereItIs")}</SelectItem>
          {queues.length > 0 && <SelectSeparator />}
          {queues.map((queue) => (
            <SelectItem key={queue.queueId} value={queue.queueId}>
              {queue.name}
              {!queue.isEnabled && " (paused)"}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );
  }

  return (
    <div className="space-y-1.5">
      <Input
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value.trim() || null)}
        placeholder={t("escalations.queueIdOrLeaveEmpty")}
        disabled={disabled}
        className={`font-mono text-xs ${className}`}
      />
      <p className="text-xs text-slate-500">
        {canListQueues
          ? "The queue list couldn't be read, so this takes an ID. Paste the queue's ID, or clear it to leave the case where it is."
          : "Listing this organization's queues needs the queues:read permission, which you don't hold — so this takes an ID rather than offering a list. Paste the queue's ID, or clear it to leave the case where it is."}
      </p>
    </div>
  );
}

export function useQueueNames(
  organizationId: string
): (queueId: string) => string | null {
  const { holds, resolved } = usePermissions();
  const canListQueues = holds("queues:read");

  const [names, setNames] = useState<ReadonlyMap<string, string>>(new Map());

  useEffect(() => {
    if (!resolved || !canListQueues) return;

    let cancelled = false;

    (async () => {
      const queues = await fetchQueuePage(organizationId);
      if (cancelled || queues === null) return;

      setNames(new Map(queues.map((queue) => [queue.queueId, queue.name])));
    })();

    return () => {
      cancelled = true;
    };
  }, [organizationId, resolved, canListQueues]);

  return useCallback((queueId: string) => names.get(queueId) ?? null, [names]);
}
