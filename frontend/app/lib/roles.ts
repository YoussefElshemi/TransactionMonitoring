"use client";

import { useEffect, useState } from "react";

import {
  PagedMetadata,
  PermissionCatalogueEntry,
  PermissionScopeType,
  Queue,
  QueueResource,
  Role,
  RoleAssignment,
  RoleResource,
  RoleUsage,
  RuleSet,
  RuleSetResource,
  SubOrganization,
  SubOrganizationResource,
} from "@/types";
import { API_BASE, unwrap, unwrapPage } from "@/lib/api";
import { resourceLabel, splitPermission } from "@/lib/permissions";

import { apiFetch } from "@/lib/http";

const LOOKUP_PAGE_SIZE = 100;

const unnest = <T,>(payload: unknown, key: string, idField: keyof T): T => {
  const record = payload as Record<string, unknown>;
  if (record && idField in record) return record as T;
  return (record?.[key] ?? record) as T;
};

export const readRole = (payload: unknown): Role =>
  unnest<Role>(payload, "role", "roleId");

export const readSubOrganization = (payload: unknown): SubOrganization =>
  unnest<SubOrganization>(payload, "subOrganization", "subOrganizationId");

export const readRoleAssignment = (payload: unknown): RoleAssignment =>
  unnest<RoleAssignment>(payload, "roleAssignment", "roleAssignmentId");

interface Lookup<T> {
  data: T;
  loading: boolean;
  unavailable: boolean;
  truncated: boolean;
  totalCount: number | null;
  reload: () => void;
}

interface LookupPage<T> {
  value: T;
  truncated: boolean;
  total: number | null;
}

const useLookup = <T,>(
  url: string | null,
  read: (response: Response) => Promise<LookupPage<T>>,
  empty: T
): Lookup<T> => {
  const [data, setData] = useState<T>(empty);
  const [loading, setLoading] = useState(true);
  const [unavailable, setUnavailable] = useState(false);
  const [truncated, setTruncated] = useState(false);
  const [totalCount, setTotalCount] = useState<number | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    if (!url) {
      setData(empty);
      setLoading(false);
      return;
    }

    let live = true;
    setLoading(true);

    void (async () => {
      try {
        const response = await apiFetch(url, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });

        if (!live) return;

        if (!response.ok) {
          setData(empty);
          setUnavailable(true);
          setTruncated(false);
          return;
        }

        const page = await read(response);
        setData(page.value);
        setTruncated(page.truncated);
        setTotalCount(page.total);
        setUnavailable(false);
      } catch {
        if (!live) return;
        setData(empty);
        setUnavailable(true);
        setTruncated(false);
      } finally {
        if (live) setLoading(false);
      }
    })();

    return () => {
      live = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [url, reloadKey]);

  return {
    data,
    loading,
    unavailable,
    truncated,
    totalCount,
    reload: () => setReloadKey((k) => k + 1),
  };
};

const pageOf = <T,>(
  page: { metadata: PagedMetadata; data: unknown[] },
  map: (row: never) => T
): LookupPage<T[]> => ({
  value: (page.data as never[]).map(map),
  truncated: page.metadata.hasMore,
  total: page.metadata.totalCount,
});

export const usePermissionCatalogue = (enabled: boolean) =>
  useLookup<PermissionCatalogueEntry[]>(
    enabled ? `${API_BASE}/permissions` : null,
    async (response) => {
      const { data } = (await response.json()) as {
        data: PermissionCatalogueEntry[];
      };
      return { value: data, truncated: false, total: data.length };
    },
    []
  );

export interface PermissionResourceGroup {
  resource: string;
  label: string;
  entries: PermissionCatalogueEntry[];
}

export const groupByResource = (
  entries: PermissionCatalogueEntry[]
): PermissionResourceGroup[] => {
  const groups: PermissionResourceGroup[] = [];

  for (const entry of entries) {
    const resource = entry.resource || splitPermission(entry.name).resource;
    const last = groups[groups.length - 1];

    if (last && last.resource === resource) {
      last.entries.push(entry);
    } else {
      groups.push({
        resource,
        label: resourceLabel(resource),
        entries: [entry],
      });
    }
  }

  return groups;
};

export const rolesPath = (organizationId: string) =>
  `${API_BASE}/organizations/${organizationId}/roles`;

export const useRoles = (
  organizationId: string | null | undefined,
  enabled: boolean
) =>
  useLookup<Role[]>(
    organizationId && enabled
      ? `${rolesPath(organizationId)}?PageNumber=1&PageSize=${LOOKUP_PAGE_SIZE}`
      : null,
    async (response) => pageOf(await unwrapPage<RoleResource>(response), readRole),
    []
  );

export const fetchRoleUsage = async (
  organizationId: string,
  roleId: string
): Promise<RoleUsage | null> => {
  try {
    const response = await apiFetch(
      `${rolesPath(organizationId)}/${roleId}/usages`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      }
    );
    if (!response.ok) return null;
    return await unwrap<RoleUsage>(response);
  } catch {
    return null;
  }
};

export const RESERVED_ROLE_NAMES = ["User", "Manager", "Admin", "Auditor"];

export const isReservedRoleName = (name: string): boolean =>
  RESERVED_ROLE_NAMES.some(
    (reserved) => reserved.toLowerCase() === name.trim().toLowerCase()
  );

export const ROLE_NAME_MAX = 64;
export const ROLE_DESCRIPTION_MAX = 512;

export const roleBlockedReason = (
  name: string,
  description: string,
  permissions: string[]
): string | null => {
  const trimmed = name.trim();
  if (!trimmed) return "A role needs a name.";
  if (trimmed.length > ROLE_NAME_MAX)
    return `The name can be at most ${ROLE_NAME_MAX} characters.`;
  if (isReservedRoleName(trimmed))
    return `${trimmed} is a built-in role, so the name is reserved. Pick another.`;
  if (description.length > ROLE_DESCRIPTION_MAX)
    return `The description can be at most ${ROLE_DESCRIPTION_MAX} characters.`;
  if (permissions.length === 0)
    return "A role has to carry at least one permission.";
  return null;
};

export const subOrganizationsPath = (organizationId: string) =>
  `${API_BASE}/organizations/${organizationId}/sub-organizations`;

export const useSubOrganizations = (
  organizationId: string | null | undefined,
  enabled: boolean
) =>
  useLookup<SubOrganization[]>(
    organizationId && enabled
      ? `${subOrganizationsPath(organizationId)}?PageNumber=1&PageSize=${LOOKUP_PAGE_SIZE}`
      : null,
    async (response) =>
      pageOf(
        await unwrapPage<SubOrganizationResource>(response),
        readSubOrganization
      ),
    []
  );

export const SUB_ORGANIZATION_NAME_MAX = 128;
export const SUB_ORGANIZATION_DESCRIPTION_MAX = 512;

export const useQueueOptions = (
  organizationId: string | null | undefined,
  enabled: boolean
) =>
  useLookup<Queue[]>(
    organizationId && enabled
      ? `${API_BASE}/organizations/${organizationId}/queues?PageNumber=1&PageSize=${LOOKUP_PAGE_SIZE}`
      : null,
    async (response) =>
      pageOf(
        await unwrapPage<QueueResource>(response),
        ({ queue }: QueueResource) => queue
      ),
    []
  );

export const useRuleSetOptions = (enabled: boolean) =>
  useLookup<RuleSet[]>(
    enabled
      ? `${API_BASE}/rule-sets?PageNumber=1&PageSize=${LOOKUP_PAGE_SIZE}`
      : null,
    async (response) =>
      pageOf(
        await unwrapPage<RuleSetResource>(response),
        ({ ruleSet }: RuleSetResource) => ruleSet
      ),
    []
  );

export const SCOPE_LOOKUP_PERMISSIONS: Record<
  Exclude<PermissionScopeType, "Organization">,
  string
> = {
  SubOrganization: "sub-organizations:read",
  RuleSet: "rule-sets:read",
  Queue: "queues:read",
};
