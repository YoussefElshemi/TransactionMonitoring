"use client";

import { useEffect, useState } from "react";

import { Party, PartyResource } from "@/types";
import { API_BASE, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { mapBounded } from "@/lib/rateLimit";
import { registerModeScopedCache } from "@/lib/mode";

const cache = new Map<string, Party>();
let cacheGeneration = 0;

const cached = (ids: readonly string[]): Map<string, Party> => {
  const known = new Map<string, Party>();
  for (const id of ids) {
    const party = cache.get(id);
    if (party) known.set(id, party);
  }
  return known;
};

const fetchParty = async (partyId: string): Promise<Party | null> => {
  try {
    const response = await apiFetch(`${API_BASE}/parties/${partyId}`, {
      method: "GET",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
    });

    if (!response.ok) return null;

    const resource = await unwrap<PartyResource>(response);
    return resource.party;
  } catch {
    return null;
  }
};

registerModeScopedCache(() => {
  cache.clear();
  cacheGeneration += 1;
});

export const useParties = (
  ids: readonly (string | null)[]
): ReadonlyMap<string, Party> => {
  // Joined rather than passed by reference: the caller builds a fresh array on
  // every render, and depending on the array itself would refetch forever.
  const key = [...new Set(ids.filter((id): id is string => id !== null))]
    .sort()
    .join(",");

  const [parties, setParties] = useState<ReadonlyMap<string, Party>>(() =>
    cached(key ? key.split(",") : [])
  );

  useEffect(() => {
    const wanted = key ? key.split(",") : [];

    if (wanted.length === 0) {
      setParties(new Map());
      return;
    }

    setParties(cached(wanted));

    const missing = wanted.filter((id) => !cache.has(id));
    if (missing.length === 0) return;

    let live = true;
    const generation = cacheGeneration;

    void (async () => {
      const fetched = await mapBounded(missing, fetchParty);

      for (const party of fetched) {
        if (party && generation === cacheGeneration) cache.set(party.partyId, party);
      }

      if (live && generation === cacheGeneration) setParties(cached(wanted));
    })();

    return () => {
      live = false;
    };
  }, [key]);

  return parties;
};

export const useParty = (partyId: string | null): Party | null => {
  const parties = useParties([partyId]);
  return partyId ? (parties.get(partyId) ?? null) : null;
};
