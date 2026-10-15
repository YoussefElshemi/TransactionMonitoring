"use client";

import { useCallback, useRef } from "react";

import { newIdempotencyKey } from "@/lib/http";

export interface IdempotentIntent {
  current: (intentId?: string) => string;
  completed: (intentId?: string) => void;
}

const SINGLE = "";

export const useIdempotencyKey = (): IdempotentIntent => {
  const keys = useRef<Map<string, string>>(new Map());

  const current = useCallback((intentId: string = SINGLE) => {
    const existing = keys.current.get(intentId);
    if (existing !== undefined) return existing;

    const minted = newIdempotencyKey();
    keys.current.set(intentId, minted);

    return minted;
  }, []);

  const completed = useCallback((intentId: string = SINGLE) => {
    keys.current.delete(intentId);
  }, []);

  return { current, completed };
};
