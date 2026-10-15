"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Plus, Undo2 } from "lucide-react";

import {
  ApplicationUser,
  Paged,
  Queue,
  RoleAssignment,
  RoleAssignmentResource,
  RuleSet,
  SubOrganization,
  UserResource,
} from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import {
  readRoleAssignment,
  useQueueOptions,
  useRoles,
  useRuleSetOptions,
  useSubOrganizations,
} from "@/lib/roles";
import { relationText, shortId, userName } from "@/lib/resources";
import { usePermissions } from "@/context/PermissionsContext";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { BuiltInBadge, ScopeBadge } from "@/components/roles/RoleParts";
import {
  EMPTY_SCOPE,
  ScopePicker,
  ScopeValue,
  scopeBody,
  scopeIncomplete,
} from "@/components/roles/ScopePicker";
import { UserCombobox } from "@/components/UserCombobox";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { apiFetch } from "@/lib/http";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function RoleAssignmentsPanel({
  organizationId,
  userId,
  roleHref,
  userHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationId: string;
  userId?: string;
  roleHref?: (roleId: string) => string;
  userHref?: (userId: string) => string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const { holds } = usePermissions();
  const canAssign = holds("roles:assign");

  const [assignments, setAssignments] = useState<RoleAssignment[]>([]);
  const [loading, setLoading] = useState(true);
  const [roleFilter, setRoleFilter] = useState("All");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [assignOpen, setAssignOpen] = useState(false);
  const [revokeTarget, setRevokeTarget] = useState<RoleAssignment | null>(null);

  const roles = useRoles(organizationId, true);
  const units = useSubOrganizations(organizationId, holds("sub-organizations:read"));
  const ruleSets = useRuleSetOptions(holds("rule-sets:read"));
  const queues = useQueueOptions(organizationId, holds("queues:read"));
  const directory = useOrganizationDirectory(!userId);

  const fetchAssignments = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (!userId && roleFilter !== "All") query.set("RoleId", roleFilter);

      const path = userId
        ? `${API_BASE}/organizations/${organizationId}/users/${userId}/role-assignments`
        : `${API_BASE}/organizations/${organizationId}/role-assignments`;

      const response = await apiFetch(`${path}?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<RoleAssignmentResource>(response);
        setAssignments(page.data.map(readRoleAssignment));
        setTotalPages(page.metadata.pageCount);
        setTotalCount(page.metadata.totalCount);
      } else {
        await reportApiError(response, {
          fallback: t("roles.errorOccurredFetchingRoleAssignments"),
          onUnauthorized,
          toastId: "error-fetching-assignments",
        });
      }
    } catch {
      toast.error(t("roles.errorOccurredFetchingRoleAssignments"), {
        id: "error-fetching-assignments",
      });
    } finally {
      setLoading(false);
    }
  }, [
    organizationId,
    userId,
    roleFilter,
    pageNumber,
    pageSize,
    onUnauthorized,
  ]);

  useEffect(() => {
    fetchAssignments();
  }, [fetchAssignments]);

  const handleRevoke = async (assignment: RoleAssignment) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/role-assignments/${assignment.roleAssignmentId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("roles.grantRevoked"));
        setRevokeTarget(null);
        fetchAssignments();
      } else {
        await reportApiError(response, {
          fallback: t("roles.failedRevokeGrant"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("roles.failedRevokeGrant"));
    }
  };

  const scopeName = useMemo(() => {
    const byId = <T,>(items: T[], key: (item: T) => string, label: (item: T) => string) =>
      new Map(items.map((item) => [key(item), label(item)]));

    const unitNames = byId<SubOrganization>(
      units.data,
      (unit) => unit.subOrganizationId,
      (unit) => unit.name
    );
    const ruleSetNames = byId<RuleSet>(
      ruleSets.data,
      (ruleSet) => ruleSet.ruleSetId,
      (ruleSet) => ruleSet.friendlyName
    );
    const queueNames = byId<Queue>(
      queues.data,
      (queue) => queue.queueId,
      (queue) => queue.name
    );

    return (assignment: RoleAssignment): string | null => {
      const { scope } = assignment;
      switch (scope.scopeType) {
        case "SubOrganization":
          return unitNames.get(scope.subOrganizationId ?? "") ?? null;
        case "RuleSet":
          return ruleSetNames.get(scope.ruleSetId ?? "") ?? null;
        case "Queue":
          return queueNames.get(scope.queueId ?? "") ?? null;
        default:
          return null;
      }
    };
  }, [units.data, ruleSets.data, queues.data]);

  const roleName = useMemo(() => {
    const names = new Map(roles.data.map((role) => [role.roleId, role]));
    return (assignment: RoleAssignment) => {
      const role = names.get(assignment.roleId);
      return {
        label: role?.name ?? assignment.roleName ?? shortId(assignment.roleId),
        isBuiltIn: role?.isBuiltIn ?? false,
      };
    };
  }, [roles.data]);

  const personName = useMemo(() => {
    const names = new Map(
      directory.users.map((person) => [person.userId, person])
    );
    return (id: string) =>
      relationText(userName(names.get(id)), id);
  }, [directory.users]);

  const assignButton = canAssign ? (
    <Button size="sm" className="gap-1.5" onClick={() => setAssignOpen(true)}>
      <Plus className="size-4" />
      {t("roles.grantARole")}
    </Button>
  ) : null;

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("roles.access")}
          title={userId ? "Access" : t("roles.roleAssignments")}
          description={t("roles.everyGrantAndHowFar")}
        >
          {assignButton}
        </PageHeader>
      )}

      {embedded && (
        <p className="text-sm text-slate-500">
          {userId
            ? "Every role this person holds, and how far each one reaches. This is the answer to “why can they see that?”."
            : "Who holds which role, and over what. A role says what somebody may do; the reach says where — and the same role granted twice with different reaches is two different levels of access."}
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        {!userId ? (
          <div className="flex flex-wrap items-center gap-3">
            <Select
              value={roleFilter}
              onValueChange={(value) => {
                setRoleFilter(value);
                setPageNumber(1);
              }}
            >
              <SelectTrigger className="w-64">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="All">{t("roles.allRoles")}</SelectItem>
                {roles.data.map((role) => (
                  <SelectItem key={role.roleId} value={role.roleId}>
                    {role.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {roles.truncated && (
              <p className="text-xs text-slate-500">
                Filtering by the first {roles.data.length} roles of{" "}
                {roles.totalCount ?? "more"}.
              </p>
            )}
          </div>
        ) : (
          <span />
        )}
        <div className="flex items-center gap-3">
          {!userId && (
            <PageSizeSelect
              value={pageSize}
              onChange={(next) => {
                setPageSize(next);
                setPageNumber(1);
              }}
            />
          )}
          {embedded && assignButton}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              {!userId && <TableHead>{t("roles.person")}</TableHead>}
              <TableHead>{t("roles.role")}</TableHead>
              <TableHead>{t("roles.reaches")}</TableHead>
              <TableHead className="w-44">{t("roles.granted")}</TableHead>
              <TableHead className="w-28">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 4 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: userId ? 4 : 5 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : assignments.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={userId ? 4 : 5}
                  className="py-14 text-center text-sm text-slate-500"
                >
                  {userId
                    ? "No roles granted. This account can sign in and reach nothing else."
                    : "Nobody has been granted a role here yet."}
                </TableCell>
              </TableRow>
            ) : (
              assignments.map((assignment) => {
                const role = roleName(assignment);
                return (
                  <TableRow
                    key={assignment.roleAssignmentId}
                    className="hover:bg-slate-50/50"
                  >
                    {!userId && (
                      <TableCell className="text-sm text-slate-700">
                        {userHref ? (
                          <Link
                            href={userHref(assignment.userId)}
                            className="text-brand-800 hover:underline"
                          >
                            {personName(assignment.userId)}
                          </Link>
                        ) : (
                          personName(assignment.userId)
                        )}
                      </TableCell>
                    )}
                    <TableCell>
                      <span className="flex items-center gap-2">
                        {roleHref ? (
                          <Link
                            href={roleHref(assignment.roleId)}
                            className="font-mono text-sm text-brand-800 hover:underline"
                          >
                            {role.label}
                          </Link>
                        ) : (
                          <span className="font-mono text-sm text-slate-900">
                            {role.label}
                          </span>
                        )}
                        {role.isBuiltIn && <BuiltInBadge />}
                      </span>
                    </TableCell>
                    <TableCell>
                      <ScopeBadge
                        scope={assignment.scope}
                        name={scopeName(assignment)}
                      />
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      <span title={format.instant(assignment.createdAt)}>
                        {format.instant(assignment.createdAt, "date")}
                      </span>
                      <span className="block font-mono text-[11px] text-slate-400">
                        by {personName(assignment.assignedByUserId)}
                      </span>
                    </TableCell>
                    <TableCell>
                      {canAssign && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="gap-1.5 text-critical-500 hover:text-critical-700"
                          onClick={() => setRevokeTarget(assignment)}
                        >
                          <Undo2 className="size-4" />
                          {t("roles.revoke")}
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {assignments.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      {assignments.length > 0 && (
        <p className="font-mono text-xs text-slate-400">
          {totalCount} {totalCount === 1 ? "grant" : "grants"}
          {userId ? " held by this person" : " in this organisation"}
        </p>
      )}

      <AssignRoleDialog
        open={assignOpen}
        onOpenChange={setAssignOpen}
        organizationId={organizationId}
        fixedUserId={userId}
        roles={roles.data}
        rolesTruncated={roles.truncated}
        onGranted={() => {
          setAssignOpen(false);
          fetchAssignments();
        }}
        onUnauthorized={onUnauthorized}
      />

      <Dialog
        open={revokeTarget !== null}
        onOpenChange={(open) => !open && setRevokeTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("roles.revokeThisGrant")}</DialogTitle>
            <DialogDescription>
              {revokeTarget && (
                <>
                  {personName(revokeTarget.userId)} loses{" "}
                  <span className="font-mono">
                    {roleName(revokeTarget).label}
                  </span>{" "}
                  over{" "}
                  {revokeTarget.scope.scopeType === "Organization"
                    ? "the whole organisation"
                    : (scopeName(revokeTarget) ?? "that resource")}
                  . They keep every other grant they hold.
                </>
              )}
            </DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            The row is removed outright — there is nothing to restore. Granting
            it again makes a new record, with you as the grantor. What happened
            here stays in the audit trail either way.
          </p>

          <DialogFooter>
            <Button variant="outline" onClick={() => setRevokeTarget(null)}>
              {t("common.cancel")}
            </Button>
            {revokeTarget && (
              <Button
                variant="destructive"
                onClick={() => handleRevoke(revokeTarget)}
              >
                {t("roles.revoke")}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function AssignRoleDialog({
  open,
  onOpenChange,
  organizationId,
  fixedUserId,
  roles,
  rolesTruncated,
  onGranted,
  onUnauthorized,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  organizationId: string;
  fixedUserId?: string;
  roles: { roleId: string; name: string; isBuiltIn: boolean }[];
  rolesTruncated: boolean;
  onGranted: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [roleId, setRoleId] = useState("");
  const [scope, setScope] = useState<ScopeValue>(EMPTY_SCOPE);
  const [selectedUser, setSelectedUser] = useState<ApplicationUser | null>(null);
  const [manualUserId, setManualUserId] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [saving, setSaving] = useState(false);

  const directory = useOrganizationDirectory(open && !fixedUserId, searchTerm);

  useEffect(() => {
    if (open) return;
    setRoleId("");
    setScope(EMPTY_SCOPE);
    setSelectedUser(null);
    setManualUserId("");
    setSearchTerm("");
  }, [open]);

  const targetUserId =
    fixedUserId ?? selectedUser?.userId ?? manualUserId.trim();

  const blocked = !roleId
    ? "Choose a role."
    : !targetUserId
      ? "Choose who it is for."
      : scopeIncomplete(scope)
        ?
          "Name the resource this reaches, or set it back to the whole organisation."
        : null;

  const handleAssign = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${targetUserId}/role-assignments`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ roleId, ...scopeBody(scope) }),
        }
      );

      if (response.ok) {
        toast.success(t("roles.roleGranted"));
        onGranted();
      } else {
        await reportApiError(response, {
          fallback: t("roles.failedGrantRole"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("roles.failedGrantRole"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("roles.grantARole")}</DialogTitle>
          <DialogDescription>
            A role says what somebody may do. The reach says where — and it
            defaults to everything, so it is worth a look even when the answer
            is the whole organisation.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {!fixedUserId && (
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("roles.person")}
              </label>
              {directory.readable ? (
                <UserCombobox
                  users={directory.users}
                  loading={directory.loading}
                  value={selectedUser}
                  onChange={setSelectedUser}
                  onSearchTermChange={setSearchTerm}
                  disabled={saving}
                  placeholder={t("roles.searchByNameOrEmail")}
                />
              ) : (
                <>
                  <Input
                    value={manualUserId}
                    onChange={(event) => setManualUserId(event.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                    disabled={saving}
                    className="font-mono text-xs"
                  />
                  <p className="text-xs text-slate-500">
                    The directory could not be read — that needs{" "}
                    <span className="font-mono">users:read</span>, which granting
                    a role does not include. The user id works just as well.
                  </p>
                </>
              )}
            </div>
          )}

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("roles.role")}
            </label>
            <Select value={roleId} onValueChange={setRoleId} disabled={saving}>
              <SelectTrigger>
                <SelectValue placeholder={t("roles.chooseARole")} />
              </SelectTrigger>
              <SelectContent>
                {roles.map((role) => (
                  <SelectItem key={role.roleId} value={role.roleId}>
                    {role.name}
                    {role.isBuiltIn && (
                      <span className="ml-2 font-mono text-[10px] uppercase tracking-wider text-slate-400">
                        built in
                      </span>
                    )}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {rolesTruncated && (
              <p className="text-xs text-slate-500">
                Showing the first {roles.length} roles. If the one you want is
                not here, narrow the list on the Roles screen and grant it from
                there.
              </p>
            )}
            <p className="text-xs text-slate-500">
              {t("roles.adminCannotBeGranted", t("roles.admin"), "platform:administer")}
            </p>
          </div>

          <ScopePicker
            organizationId={organizationId}
            value={scope}
            onChange={setScope}
            disabled={saving}
          />
        </div>

        <DialogFooter>
          {blocked && (
            <p className="mr-auto self-center text-xs text-slate-500">
              {blocked}
            </p>
          )}
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={saving}
          >
            {t("common.cancel")}
          </Button>
          <Button onClick={handleAssign} disabled={saving || blocked !== null}>
            {saving ? "Granting..." : "Grant"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function useOrganizationDirectory(enabled: boolean, searchTerm = "") {
  const [users, setUsers] = useState<ApplicationUser[]>([]);
  const [readable, setReadable] = useState(true);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!enabled) return;

    let cancelled = false;
    setLoading(true);

    void (async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/my/users?PageNumber=1&PageSize=100&UserName=${encodeURIComponent(searchTerm)}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (cancelled) return;

        if (response.ok) {
          const page = (await response.json()) as Paged<UserResource>;
          setUsers(page.data.map((resource) => resource.user));
          setReadable(true);
        } else if (response.status === 403 || response.status === 404) {
          setReadable(false);
        } else {
          setUsers([]);
        }
      } catch {
        if (!cancelled) setUsers([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [enabled, searchTerm]);

  return { users, readable, loading };
}
