"use client";

import { useCallback, useEffect, useState } from "react";

import { Queue, QueueResource } from "@/types";
import { API_BASE, unwrapPage } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
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

export const QUEUE_ANY = "Any";
export const QUEUE_UNROUTED = "Unrouted";

export function QueueFilterSelect({
  value,
  onChange,
  className = "w-52",
}: {
  value: string;
  onChange: (value: string) => void;
  className?: string;
}) {
  const t = useTranslation();
  const { user } = useAuth();
  const { holds, resolved } = usePermissions();
  const canListOrganizationQueues = holds("queues:read");
  const [queues, setQueues] = useState<Queue[]>([]);

  const fetchQueues = useCallback(async () => {
    if (!user) return;

    const url = canListOrganizationQueues
      ? `${API_BASE}/organizations/${user.organizationId}/queues?PageNumber=1&PageSize=100`
      : `${API_BASE}/queues/my?PageNumber=1&PageSize=100`;

    try {
      const response = await apiFetch(url, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (!response.ok) {
        setQueues([]);
        return;
      }

      const page = await unwrapPage<QueueResource>(response);
      setQueues(page.data.map(({ queue }) => queue));
    } catch {
      setQueues([]);
    }
  }, [user, canListOrganizationQueues]);

  useEffect(() => {
    if (resolved) fetchQueues();
  }, [resolved, fetchQueues]);

  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger className={className}>
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={QUEUE_ANY}>{t("queues.anyQueue")}</SelectItem>
        <SelectItem value={QUEUE_UNROUTED}>{t("queues.unrouted")}</SelectItem>
        {queues.length > 0 && <SelectSeparator />}
        {queues.map((queue) => (
          <SelectItem key={queue.queueId} value={queue.queueId}>
            {queue.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
