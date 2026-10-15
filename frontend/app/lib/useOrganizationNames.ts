"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import type { Organization } from "@/types";
import { API_BASE, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";

export const useOrganizationNames = (
  organizationIds: readonly string[]
): ((organizationId: string) => string | null) => {
  const cache = useRef<Map<string, string>>(new Map());
  const inFlight = useRef<Set<string>>(new Set());
  const [, setResolved] = useState(0);

  // Joined rather than passed as an array: a caller derives this list inside
  // its own render, so the array identity changes on every pass and an effect
  // keyed on it would never stop running.
  const key = organizationIds.join("|");

  const fetchNames = useCallback(async (ids: readonly string[]) => {
    const wanted = ids.filter(
      (id) => id && !cache.current.has(id) && !inFlight.current.has(id)
    );
    if (wanted.length === 0) return;

    for (const id of wanted) inFlight.current.add(id);

    await Promise.all(
      wanted.map(async (id) => {
        try {
          const response = await apiFetch(`${API_BASE}/organizations/${id}`, {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          });

          if (response.ok) {
            const organization = await unwrap<Organization>(response);
            cache.current.set(id, organization.friendlyName);
          }
        } catch {
        } finally {
          inFlight.current.delete(id);
        }
      })
    );

    setResolved((count) => count + 1);
  }, []);

  useEffect(() => {
    const ids = key ? key.split("|") : [];
    if (ids.length > 0) fetchNames(ids);
  }, [key, fetchNames]);

  return useCallback(
    (organizationId: string) => cache.current.get(organizationId) ?? null,
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [key, cache.current.size]
  );
};
