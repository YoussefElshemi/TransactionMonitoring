"use client";

import { useEffect, useState } from "react";

import { SchemaField, TransactionSchemaResource } from "@/types";
import { API_BASE } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { registerModeScopedCache } from "@/lib/mode";

export type TransactionSchemaStatus =
  | "loading"
  | "declared"
  | "absent"
  | "unavailable";

export interface TransactionSchemaState {
  status: TransactionSchemaStatus;
  fields: SchemaField[] | null;
}

type Loaded = { status: Exclude<TransactionSchemaStatus, "loading">; fields: SchemaField[] | null };

let cached: Promise<Loaded> | null = null;
const listeners = new Set<() => void>();

const request = async (): Promise<Loaded> => {
  try {
    const response = await apiFetch(
      `${API_BASE}/organizations/my/transaction-schema`,
      { credentials: "include" }
    );

    if (response.ok) {
      const resource = (await response.json()) as {
        data: TransactionSchemaResource;
      };
      return {
        status: "declared",
        fields: resource.data.transactionSchema.fields,
      };
    }

    if (response.status === 404) return { status: "absent", fields: null };

    return { status: "unavailable", fields: null };
  } catch {
    return { status: "unavailable", fields: null };
  }
};

const load = (): Promise<Loaded> => {
  cached ??= request();
  return cached;
};

export const invalidateTransactionSchema = (): void => {
  cached = null;
  listeners.forEach((listener) => listener());
};

registerModeScopedCache(invalidateTransactionSchema);

export const useTransactionSchema = (enabled: boolean): TransactionSchemaState => {
  const [state, setState] = useState<TransactionSchemaState>({
    status: enabled ? "loading" : "absent",
    fields: null,
  });

  useEffect(() => {
    if (!enabled) {
      setState({ status: "absent", fields: null });
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
