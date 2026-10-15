"use client";

import { useCallback, useEffect, useState } from "react";

import type { BillingProfile } from "@/types";
import { API_BASE, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { isMissingProfile } from "@/lib/billingProfiles";
import { usePermissions } from "@/context/PermissionsContext";

export interface BillingProfileState {
  profile: BillingProfile | null;
  resolved: boolean;
  missing: boolean;
  refresh: () => void;
}

export const useBillingProfile = (
  organizationId: string
): BillingProfileState => {
  const { holds, resolved: permissionsResolved } = usePermissions();
  const mayRead = holds("billing-profiles:read");

  const [profile, setProfile] = useState<BillingProfile | null>(null);
  const [resolved, setResolved] = useState(false);
  const [missing, setMissing] = useState(false);
  const [generation, setGeneration] = useState(0);

  const refresh = useCallback(() => setGeneration((at) => at + 1), []);

  useEffect(() => {
    if (!permissionsResolved) return;

    if (!mayRead) {
      setResolved(true);
      return;
    }

    let cancelled = false;

    const read = async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/billing-profile`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (cancelled) return;

        if (response.ok) {
          setProfile(await unwrap<BillingProfile>(response));
          setMissing(false);
        } else {
          setProfile(null);
          setMissing(isMissingProfile(response));
        }
      } catch {
        if (!cancelled) setProfile(null);
      } finally {
        if (!cancelled) setResolved(true);
      }
    };

    read();

    return () => {
      cancelled = true;
    };
  }, [organizationId, mayRead, permissionsResolved, generation]);

  return { profile, resolved, missing, refresh };
};
