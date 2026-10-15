"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Eye, Plus, Trash2 } from "lucide-react";

import { Role, RoleResource, RoleUsage } from "@/types";
import { reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  fetchRoleUsage,
  readRole,
  roleBlockedReason,
  ROLE_DESCRIPTION_MAX,
  ROLE_NAME_MAX,
  rolesPath,
} from "@/lib/roles";
import { usePermissions } from "@/context/PermissionsContext";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { PermissionPicker } from "@/components/permissions/PermissionPicker";
import { BuiltInBadge } from "@/components/roles/RoleParts";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
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
import { useTranslation } from "@/context/LocalizationContext";

const ANY_KIND = "All";

export function RolesPanel({
  organizationId,
  detailHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationId: string;
  detailHref: (roleId: string) => string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const { holds } = usePermissions();
  const canWrite = holds("roles:write");

  const [roles, setRoles] = useState<Role[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState("");
  const [kind, setKind] = useState(ANY_KIND);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [newName, setNewName] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newPermissions, setNewPermissions] = useState<string[]>([]);

  const [deleteTarget, setDeleteTarget] = useState<{
    role: Role;
    usage: RoleUsage | null;
  } | null>(null);

  const fetchRoles = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (searchTerm) query.set("Name", searchTerm);
      if (kind !== ANY_KIND) query.set("IsBuiltIn", String(kind === "Built in"));

      const response = await apiFetch(`${rolesPath(organizationId)}?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<RoleResource>(response);
        setRoles(page.data.map(readRole));
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("roles.errorOccurredFetchingRoles"),
          onUnauthorized,
          toastId: "error-fetching-roles",
        });
      }
    } catch {
      toast.error(t("roles.errorOccurredFetchingRoles"), {
        id: "error-fetching-roles",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, pageSize, searchTerm, kind, onUnauthorized]);

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  const handleCreate = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(rolesPath(organizationId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({
          name: newName.trim(),
          description: newDescription.trim() || null,
          permissions: newPermissions,
        }),
      });

      if (response.ok) {
        const created = readRole(await unwrap<RoleResource>(response));
        toast.success(t("common.namedCreated", created.name), {
          description:
            t("roles.nobodyHoldsYetGrantAccess"),
        });
        setCreateOpen(false);
        setNewName("");
        setNewDescription("");
        setNewPermissions([]);
        router.push(detailHref(created.roleId));
      } else {
        await reportApiError(response, {
          fallback: t("roles.failedCreateRole"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("roles.failedCreateRole"));
    } finally {
      setSaving(false);
    }
  };

  const askToDelete = async (role: Role) => {
    setDeleteTarget({ role, usage: null });
    const usage = await fetchRoleUsage(organizationId, role.roleId);
    if (usage) setDeleteTarget({ role, usage });
  };

  const handleDelete = async (role: Role) => {
    try {
      const response = await apiFetch(`${rolesPath(organizationId)}/${role.roleId}`, {
        method: "DELETE",
        credentials: "include",
      });

      if (response.ok) {
        toast.success(t("common.namedDeleted", role.name), {
          action: {
            label: t("common.undo"),
            onClick: () => handleRecover(role),
          },
        });
        setDeleteTarget(null);
        fetchRoles();
      } else {
        await reportApiError(response, {
          fallback: t("roles.failedDeleteRole"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("roles.failedDeleteRole"));
    }
  };

  const handleRecover = async (role: Role) => {
    try {
      const response = await apiFetch(
        `${rolesPath(organizationId)}/${role.roleId}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRestored", role.name), {
          description:
            t("roles.roleBackAssignmentsAreNot"),
        });
        fetchRoles();
      } else {
        await reportApiError(response, {
          fallback: t("roles.failedRecoverRole"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("roles.failedRecoverRole"));
    }
  };

  const createBlocked = roleBlockedReason(
    newName,
    newDescription,
    newPermissions
  );

  const createButton = canWrite ? (
    <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
      <Plus className="size-4" />
      {t("roles.newRole")}
    </Button>
  ) : null;

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("roles.access")}
          title={t("common.roles")}
          description={t("roles.aRoleIsANamed")}
        >
          {createButton}
        </PageHeader>
      )}

      {embedded && (
        <p className="text-sm text-slate-500">
          A role is a named bundle of permissions. How far one reaches is decided
          when it is granted — the same role can cover the whole organisation for
          one person and a single queue for another, so resist naming a role
          after a team.
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPageNumber(1);
            }}
            placeholder={t("common.searchByName")}
            className="w-56"
          />
          <Select
            value={kind}
            onValueChange={(value) => {
              setKind(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_KIND}>{t("roles.allRoles")}</SelectItem>
              <SelectItem value="Built in">{t("roles.builtIn")}</SelectItem>
              <SelectItem value="Yours">{t("roles.yours")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {embedded && createButton}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.name")}</TableHead>
              <TableHead>{t("common.description")}</TableHead>
              <TableHead className="w-32">{t("roles.permissions")}</TableHead>
              <TableHead className="w-28">{t("common.kind")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 5 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : roles.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              roles.map((role) => (
                <TableRow key={role.roleId} className="hover:bg-slate-50/50">
                  <TableCell className="font-mono text-sm font-medium text-slate-900">
                    {role.name}
                  </TableCell>
                  <TableCell className="max-w-md text-sm text-slate-600">
                    {role.description ?? (
                      <span className="text-slate-300">—</span>
                    )}
                  </TableCell>
                  <TableCell className="font-mono text-sm text-slate-600">
                    {role.permissions.length}
                  </TableCell>
                  <TableCell>
                    {role.isBuiltIn ? (
                      <BuiltInBadge />
                    ) : (
                      <span className="font-mono text-xs text-slate-400">
                        yours
                      </span>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() => router.push(detailHref(role.roleId))}
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      {canWrite && !role.isBuiltIn && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          title={t("common.delete")}
                          aria-label={t("common.delete")}
                          onClick={() => askToDelete(role)}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {roles.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("roles.newRole2")}</DialogTitle>
            <DialogDescription>
              Pick the permissions it carries. How far it reaches is chosen when
              somebody is granted it, so name this after what it lets a person
              do, not after the team that will hold it.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.name")}
              </label>
              <Input
                value={newName}
                onChange={(event) => setNewName(event.target.value)}
                placeholder={t("roles.filingReviewer")}
                maxLength={ROLE_NAME_MAX}
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                Unique in your organisation. User, Manager, Admin and Auditor are
                built-in names and cannot be reused.
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.description")}
              </label>
              <Textarea
                value={newDescription}
                onChange={(event) => setNewDescription(event.target.value)}
                placeholder={t("roles.readsAndFilesSuspiciousActivity")}
                maxLength={ROLE_DESCRIPTION_MAX}
                rows={2}
              />
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("roles.permissions")}
              </label>
              <PermissionPicker
                selected={newPermissions}
                onChange={setNewPermissions}
                disabled={saving}
              />
            </div>
          </div>

          <DialogFooter>
            {createBlocked && (
              <p className="mr-auto self-center text-xs text-slate-500">
                {createBlocked}
              </p>
            )}
            <Button
              variant="outline"
              onClick={() => setCreateOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button
              onClick={handleCreate}
              disabled={saving || createBlocked !== null}
            >
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <DeleteRoleDialog
        target={deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        onDelete={handleDelete}
      />
    </div>
  );
}

function DeleteRoleDialog({
  target,
  onOpenChange,
  onDelete,
}: {
  target: { role: Role; usage: RoleUsage | null } | null;
  onOpenChange: (open: boolean) => void;
  onDelete: (role: Role) => void;
}) {
  const t = useTranslation();
  const inUse = target?.usage === null || (target?.usage?.assignmentCount ?? 0) > 0;
  const usage = target?.usage;

  return (
    <Dialog open={target !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {inUse ? "Somebody is still on this role" : "Delete this role?"}
          </DialogTitle>
          <DialogDescription>
            {usage
              ? inUse
                ? `${usage.assignmentCount} assignment${
                    usage.assignmentCount === 1 ? "" : "s"
                  } across ${usage.userIds.length} ${
                    usage.userIds.length === 1 ? "person" : "people"
                  } would be left pointing at nothing, so the API refuses. Revoke them on the Access screen, then come back.`
                : `Nobody holds ${usage.name}, so it can be removed outright.`
              : "Whether anybody still holds this role could not be read, so deleting may be refused. Check the Access screen first."}
          </DialogDescription>
        </DialogHeader>

        {!inUse && (
          <p className="text-sm text-slate-600">
            A deleted role can be recovered, and the toast offers it — but
            recovering brings back the role, not the assignments anybody had on
            it.
          </p>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("common.cancel")}
          </Button>
          {target && !inUse && (
            <Button variant="destructive" onClick={() => onDelete(target.role)}>
              {t("common.delete")}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
