"use client";

import {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";

import { PermissionGrant } from "@/types";
import { API_BASE } from "@/lib/api";
import {
  grantsFor,
  grantsHold,
  grantsHoldOver,
  heldPermissions,
  holdsOrganizationWide,
  Permission,
  ScopeTarget,
  unreadableGrantsFallback,
} from "@/lib/permissions";
import { useAuth } from "@/context/AuthContext";

import { apiFetch } from "@/lib/http";

interface PermissionsContextType {
  grants: PermissionGrant[] | null;
  resolved: boolean;
  failed: boolean;
  holds: (permission: Permission) => boolean;
  holdsOver: (permission: Permission, target: ScopeTarget) => boolean;
  holdsAll: (...permissions: Permission[]) => boolean;
  holdsAny: (...permissions: Permission[]) => boolean;
  organizationWide: (permission: Permission) => boolean;
  grantsFor: (permission: Permission) => PermissionGrant[];
  held: Permission[];
  refresh: () => Promise<void>;
}

const PermissionsContext = createContext<PermissionsContextType | undefined>(
  undefined
);

/** Per session, per account. Keyed by user so that signing in as somebody else
 *  in the same tab cannot inherit the previous account's reach. */
const cacheKey = (userId: string) => `permissions:${userId}`;

const readCache = (userId: string): PermissionGrant[] | null => {
  try {
    const cached = sessionStorage.getItem(cacheKey(userId));
    return cached ? (JSON.parse(cached) as PermissionGrant[]) : null;
  } catch {
    return null;
  }
};

const writeCache = (userId: string, grants: PermissionGrant[]) => {
  try {
    sessionStorage.setItem(cacheKey(userId), JSON.stringify(grants));
  } catch {
  }
};

export const PermissionsProvider = ({ children }: { children: ReactNode }) => {
  const { user } = useAuth();
  const userId = user?.userId ?? null;

  const [grants, setGrants] = useState<PermissionGrant[] | null>(null);
  const [resolved, setResolved] = useState(false);
  const [failed, setFailed] = useState(false);

  const fetchGrants = useCallback(async () => {
    if (!userId) {
      setGrants(null);
      setResolved(true);
      setFailed(false);
      return;
    }

    const cached = readCache(userId);
    if (cached) {
      setGrants(cached);
      setResolved(true);
    }

    try {
      const response = await apiFetch(`${API_BASE}/permissions/mine`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (!response.ok) {
        if (!cached) setGrants(null);
        setFailed(true);
        return;
      }

      const { data } = (await response.json()) as { data: PermissionGrant[] };
      setGrants(data);
      setFailed(false);
      writeCache(userId, data);
    } catch {
      if (!cached) setGrants(null);
      setFailed(true);
    } finally {
      setResolved(true);
    }
  }, [userId]);

  useEffect(() => {
    setResolved(false);
    fetchGrants();
  }, [fetchGrants]);

  const effective = useMemo(
    () => grants ?? (resolved ? unreadableGrantsFallback(user) : null),
    [grants, resolved, user]
  );

  const value = useMemo<PermissionsContextType>(
    () => ({
      grants: effective,
      resolved,
      failed,
      holds: (permission) => grantsHold(effective, permission),
      holdsOver: (permission, target) =>
        grantsHoldOver(effective, permission, target),
      holdsAll: (...permissions) =>
        permissions.every((permission) => grantsHold(effective, permission)),
      holdsAny: (...permissions) =>
        permissions.some((permission) => grantsHold(effective, permission)),
      organizationWide: (permission) =>
        holdsOrganizationWide(effective, permission),
      grantsFor: (permission) => grantsFor(effective, permission),
      held: heldPermissions(effective),
      refresh: fetchGrants,
    }),
    [effective, resolved, failed, fetchGrants]
  );

  return (
    <PermissionsContext.Provider value={value}>
      {children}
    </PermissionsContext.Provider>
  );
};

export const usePermissions = () => {
  const context = useContext(PermissionsContext);
  if (!context) {
    throw new Error("usePermissions must be used within a PermissionsProvider");
  }
  return context;
};
