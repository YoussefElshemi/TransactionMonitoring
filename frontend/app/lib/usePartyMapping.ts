"use client";

import { useEffect, useState } from "react";

import { PartyMappingEntry, PartyMappingResource } from "@/types";
import { API_BASE } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { registerModeScopedCache } from "@/lib/mode";

export type PartyMappingStatus =
  | "loading"
  | "declared"
  | "absent"
  | "unavailable";

export interface PartyMappingState {
  status: PartyMappingStatus;
  mappings: PartyMappingEntry[] | null;
}

type Loaded = {
  status: Exclude<PartyMappingStatus, "loading">;
  mappings: PartyMappingEntry[] | null;
};

let cached: Promise<Loaded> | null = null;
const listeners = new Set<() => void>();

const request = async (): Promise<Loaded> => {
  try {
    const response = await apiFetch(`${API_BASE}/organizations/my/party-mapping`, {
      credentials: "include",
    });

    if (response.ok) {
      const resource = (await response.json()) as {
        data: PartyMappingResource;
      };
      return {
        status: "declared",
        mappings: resource.data.partyMapping.mappings,
      };
    }

    if (response.status === 404) return { status: "absent", mappings: null };

    return { status: "unavailable", mappings: null };
  } catch {
    return { status: "unavailable", mappings: null };
  }
};

const load = (): Promise<Loaded> => {
  cached ??= request();
  return cached;
};

export const invalidatePartyMapping = (): void => {
  cached = null;
  listeners.forEach((listener) => listener());
};

registerModeScopedCache(invalidatePartyMapping);

export const usePartyMapping = (enabled: boolean): PartyMappingState => {
  const [state, setState] = useState<PartyMappingState>({
    status: enabled ? "loading" : "absent",
    mappings: null,
  });

  useEffect(() => {
    if (!enabled) {
      setState({ status: "absent", mappings: null });
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
