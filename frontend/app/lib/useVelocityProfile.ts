"use client";

import { useEffect, useState } from "react";

import {
  VelocityMetric,
  VelocityProfileResource,
  VelocityThreshold,
} from "@/types";
import { API_BASE } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { registerModeScopedCache } from "@/lib/mode";

export type VelocityProfileStatus =
  | "loading"
  | "declared"
  | "absent"
  | "unavailable";

export interface VelocityProfileState {
  status: VelocityProfileStatus;
  metrics: VelocityMetric[] | null;
  thresholds: VelocityThreshold[] | null;
}

type Loaded = {
  status: Exclude<VelocityProfileStatus, "loading">;
  metrics: VelocityMetric[] | null;
  thresholds: VelocityThreshold[] | null;
};

const NOTHING = { metrics: null, thresholds: null } as const;

let cached: Promise<Loaded> | null = null;
const listeners = new Set<() => void>();

const request = async (): Promise<Loaded> => {
  try {
    const response = await apiFetch(
      `${API_BASE}/organizations/my/velocity-profile`,
      { credentials: "include" }
    );

    if (response.ok) {
      const resource = (await response.json()) as {
        data: VelocityProfileResource;
      };
      return {
        status: "declared",
        metrics: resource.data.velocityProfile.metrics,
        thresholds: resource.data.velocityProfile.thresholds ?? [],
      };
    }

    if (response.status === 404) return { status: "absent", ...NOTHING };

    return { status: "unavailable", ...NOTHING };
  } catch {
    return { status: "unavailable", ...NOTHING };
  }
};

const load = (): Promise<Loaded> => {
  cached ??= request();
  return cached;
};

export const invalidateVelocityProfile = (): void => {
  cached = null;
  listeners.forEach((listener) => listener());
};

registerModeScopedCache(invalidateVelocityProfile);

export const useVelocityProfile = (enabled: boolean): VelocityProfileState => {
  const [state, setState] = useState<VelocityProfileState>({
    status: enabled ? "loading" : "absent",
    ...NOTHING,
  });

  useEffect(() => {
    if (!enabled) {
      setState({ status: "absent", ...NOTHING });
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
