"use client";

import { useCallback, useEffect, useState } from "react";

import { CaseDisposition, CaseReasonCode, CaseReasonCodeResource } from "@/types";
import { API_BASE, unwrapPage } from "@/lib/api";

import { apiFetch } from "@/lib/http";

const PAGE_SIZE = 100;

export const appliesTo = (
  reasonCode: CaseReasonCode,
  disposition: CaseDisposition | null
): boolean =>
  disposition === null ||
  reasonCode.disposition === null ||
  reasonCode.disposition === disposition;

export interface ReasonCodeTaxonomy {
  codes: CaseReasonCode[];
  loading: boolean;
  unavailable: boolean;
}

export const useReasonCodes = (organizationId: string | null | undefined): ReasonCodeTaxonomy => {
  const [codes, setCodes] = useState<CaseReasonCode[]>([]);
  const [loading, setLoading] = useState(true);
  const [unavailable, setUnavailable] = useState(false);

  const fetchCodes = useCallback(async () => {
    if (!organizationId) {
      setLoading(false);
      return;
    }

    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes?IsEnabled=true&PageNumber=1&PageSize=${PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (!response.ok) {
        setCodes([]);
        setUnavailable(true);
        return;
      }

      const page = await unwrapPage<CaseReasonCodeResource>(response);
      setCodes(page.data.map(({ caseReasonCode }) => caseReasonCode));
      setUnavailable(false);
    } catch {
      setCodes([]);
      setUnavailable(true);
    } finally {
      setLoading(false);
    }
  }, [organizationId]);

  useEffect(() => {
    fetchCodes();
  }, [fetchCodes]);

  return { codes, loading, unavailable };
};
