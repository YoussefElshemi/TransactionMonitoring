"use client";

import { useEffect, useState } from "react";

import { WebhookCatalogue } from "@/types";
import { API_BASE } from "@/lib/api";

import { apiFetch } from "@/lib/http";

export type WebhookCatalogueStatus =
  | "loading"
  | "loaded"
  | "unavailable";

export interface WebhookCatalogueState {
  status: WebhookCatalogueStatus;
  catalogue: WebhookCatalogue | null;
}

type Loaded = {
  status: Exclude<WebhookCatalogueStatus, "loading">;
  catalogue: WebhookCatalogue | null;
};

let cached: Promise<Loaded> | null = null;
const listeners = new Set<() => void>();

const request = async (): Promise<Loaded> => {
  try {
    const response = await apiFetch(`${API_BASE}/webhooks/catalogue`, {
      credentials: "include",
    });

    if (response.ok) {
      const body = (await response.json()) as { data: WebhookCatalogue };
      return { status: "loaded", catalogue: body.data };
    }

    return { status: "unavailable", catalogue: null };
  } catch {
    return { status: "unavailable", catalogue: null };
  }
};

const load = (): Promise<Loaded> => {
  cached ??= request();
  return cached;
};

export const invalidateWebhookCatalogue = (): void => {
  cached = null;
  listeners.forEach((listener) => listener());
};

export const useWebhookCatalogue = (
  enabled: boolean = true
): WebhookCatalogueState => {
  const [state, setState] = useState<WebhookCatalogueState>({
    status: enabled ? "loading" : "unavailable",
    catalogue: null,
  });

  useEffect(() => {
    if (!enabled) {
      setState({ status: "unavailable", catalogue: null });
      return;
    }

    let current = true;

    const read = () => {
      void load().then((loaded) => {
        if (current) setState(loaded);
      });
    };

    read();
    listeners.add(read);

    return () => {
      current = false;
      listeners.delete(read);
    };
  }, [enabled]);

  return state;
};
