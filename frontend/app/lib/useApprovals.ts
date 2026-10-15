"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import { ApprovalPolicy, ApprovalResource } from "@/types";
import { API_BASE, unwrap, unwrapPage } from "@/lib/api";
import { findCaseApproval, findDeploymentApproval } from "@/lib/approvals";

import { apiFetch } from "@/lib/http";

export const APPROVAL_COUNT_POLL_MS = 30_000;

const LOOKUP_PAGE_SIZE = 100;

export const usePendingApprovalCount = (enabled: boolean): number | null => {
  const [count, setCount] = useState<number | null>(null);

  useEffect(() => {
    if (!enabled) {
      setCount(null);
      return;
    }

    let live = true;

    const read = async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/approvals?status=Pending&pageSize=1`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );
        if (!response.ok) return;
        const page = await unwrapPage<ApprovalResource>(response);
        if (live) setCount(page.metadata.totalCount);
      } catch {
      }
    };

    read();
    const timer = setInterval(read, APPROVAL_COUNT_POLL_MS);

    return () => {
      live = false;
      clearInterval(timer);
    };
  }, [enabled]);

  return count;
};

export const useCaseApproval = (
  caseId: string | null | undefined,
  onSettled: () => void
): { approval: ApprovalResource | null; refresh: () => void } => {
  const [approval, setApproval] = useState<ApprovalResource | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  // Held in a ref so an inline callback from the caller cannot restart the
  // poll on every render.
  const settledRef = useRef(onSettled);
  settledRef.current = onSettled;

  const hadOne = useRef(false);

  const refresh = useCallback(() => setReloadKey((key) => key + 1), []);

  useEffect(() => {
    if (!caseId) {
      setApproval(null);
      hadOne.current = false;
      return;
    }

    let live = true;

    const read = async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/approvals?status=Pending&subject=CaseResolution&pageSize=${LOOKUP_PAGE_SIZE}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );
        if (!response.ok || !live) return;

        const page = await unwrapPage<ApprovalResource>(response);
        const match = findCaseApproval(page.data, caseId);
        if (!live) return;

        setApproval(match);
        if (hadOne.current && match === null) settledRef.current();
        hadOne.current = match !== null;
      } catch {
      }
    };

    read();
    const timer = setInterval(read, APPROVAL_COUNT_POLL_MS);

    return () => {
      live = false;
      clearInterval(timer);
    };
  }, [caseId, reloadKey]);

  return { approval, refresh };
};

export const useDeploymentApproval = (
  ruleDeploymentId: string | null | undefined
): ApprovalResource | null => {
  const [approval, setApproval] = useState<ApprovalResource | null>(null);

  useEffect(() => {
    if (!ruleDeploymentId) {
      setApproval(null);
      return;
    }

    let live = true;

    void (async () => {
      const found = await readDeploymentApproval(ruleDeploymentId);
      if (live) setApproval(found);
    })();

    return () => {
      live = false;
    };
  }, [ruleDeploymentId]);

  return approval;
};

export const readDeploymentApproval = async (
  ruleDeploymentId: string
): Promise<ApprovalResource | null> => {
  try {
    const response = await apiFetch(
      `${API_BASE}/approvals?status=Pending&subject=RuleDeployment&pageSize=${LOOKUP_PAGE_SIZE}`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      }
    );
    if (!response.ok) return null;

    const page = await unwrapPage<ApprovalResource>(response);
    return findDeploymentApproval(page.data, ruleDeploymentId);
  } catch {
    return null;
  }
};

export const useDeploymentApprovalRequired = (
  organizationId: string | null | undefined,
  enabled: boolean
): boolean | null => {
  const [required, setRequired] = useState<boolean | null>(null);

  useEffect(() => {
    if (!organizationId || !enabled) {
      setRequired(null);
      return;
    }

    let live = true;

    void (async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/approval-policy`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (!live) return;

        if (response.status === 404) {
          setRequired(false);
          return;
        }

        if (!response.ok) return;

        const policy = await unwrap<ApprovalPolicy>(response);
        if (live) setRequired(policy.requireDeploymentApproval);
      } catch {
      }
    })();

    return () => {
      live = false;
    };
  }, [organizationId, enabled]);

  return required;
};
