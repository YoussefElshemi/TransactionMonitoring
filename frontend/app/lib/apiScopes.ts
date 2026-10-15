"use client";

import { useEffect, useState } from "react";

import { API_BASE } from "@/lib/api";

import { apiFetch } from "@/lib/http";

export const FALLBACK_API_SCOPES: readonly string[] = [
  "rules:evaluate",
  "rulesets:evaluate",
];

const SCOPE_DESCRIPTIONS: Record<string, string> = {
  "rules:evaluate": "Evaluate one rule — POST /rules/{ruleId}/evaluate",
  "rulesets:evaluate":
    "Evaluate rule sets — POST /rule-sets/evaluate and /rule-sets/{ruleSetId}/evaluate",
};

export const scopeDescription = (scope: string): string | null =>
  SCOPE_DESCRIPTIONS[scope] ?? null;

interface OpenApiDocument {
  components?: {
    securitySchemes?: {
      OAuth2?: {
        flows?: {
          clientCredentials?: {
            scopes?: Record<string, string>;
          };
        };
      };
    };
  };
}

let cached: string[] | null = null;
let inFlight: Promise<string[]> | null = null;

const readAdvertisedScopes = async (): Promise<string[]> => {
  try {
    const response = await apiFetch(`${API_BASE}/swagger/v1/swagger.json`, {
      credentials: "include",
    });

    if (!response.ok) return [...FALLBACK_API_SCOPES];

    const document = (await response.json()) as OpenApiDocument;
    const advertised =
      document.components?.securitySchemes?.OAuth2?.flows?.clientCredentials
        ?.scopes;
    const scopes = advertised ? Object.keys(advertised) : [];

    return scopes.length ? scopes : [...FALLBACK_API_SCOPES];
  } catch {
    return [...FALLBACK_API_SCOPES];
  }
};

const loadApiScopes = (): Promise<string[]> => {
  if (cached) return Promise.resolve(cached);

  inFlight ??= readAdvertisedScopes().then((scopes) => {
    cached = scopes;
    inFlight = null;
    return scopes;
  });

  return inFlight;
};

export function useApiScopes(granted: string[] = []): string[] {
  const [advertised, setAdvertised] = useState<string[]>(
    cached ?? [...FALLBACK_API_SCOPES]
  );

  useEffect(() => {
    let current = true;
    void loadApiScopes().then((scopes) => {
      if (current) setAdvertised(scopes);
    });
    return () => {
      current = false;
    };
  }, []);

  const retired = granted.filter((scope) => !advertised.includes(scope));

  return retired.length ? [...advertised, ...retired] : advertised;
}
