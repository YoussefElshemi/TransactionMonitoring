"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, Save, Trash2 } from "lucide-react";

import { Role, RoleResource, RoleUsage } from "@/types";
import { reportApiError, unwrap } from "@/lib/api";
import {
  fetchRoleUsage,
  readRole,
  roleBlockedReason,
  ROLE_DESCRIPTION_MAX,
  ROLE_NAME_MAX,
  rolesPath,
} from "@/lib/roles";
import { usePermissions } from "@/context/PermissionsContext";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { TabBar } from "@/components/TabBar";
import { FieldList, Field } from "@/components/FieldList";
import { HistoryPanel, historyPath } from "@/components/history";
import { PermissionPicker } from "@/components/permissions/PermissionPicker";
import { BuiltInBadge, PermissionName } from "@/components/roles/RoleParts";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function RoleDetail({
  organizationId,
  roleId,
  backHref,
  assignmentsHref,
  onUnauthorized,
}: {
  organizationId: string;
  roleId: string;
  backHref: string;
  assignmentsHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const { holds, holdsAll } = usePermissions();
  const canWrite = holds("roles:write");
  const canReadHistory = holdsAll("roles:read", "audit-logs:read");

  const [role, setRole] = useState<Role | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [activeTab, setActiveTab] = useState("Permissions");

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [permissions, setPermissions] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);

  const [usage, setUsage] = useState<RoleUsage | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const fetchRole = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(`${rolesPath(organizationId)}/${roleId}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const fetched = readRole(await unwrap<RoleResource>(response));
        setRole(fetched);
        setName(fetched.name);
        setDescription(fetched.description ?? "");
        setPermissions(fetched.permissions);
        setNotFound(false);
      } else if (response.status === 404) {
        setNotFound(true);
      } else {
        await reportApiError(response, {
          fallback: t("roles.errorOccurredFetchingRole"),
          onUnauthorized,
          toastId: "error-fetching-role",
        });
      }
    } catch {
      toast.error(t("roles.errorOccurredFetchingRole"), {
        id: "error-fetching-role",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, roleId, onUnauthorized]);

  useEffect(() => {
    fetchRole();
  }, [fetchRole]);

  useEffect(() => {
    if (!role) return;
    let live = true;
    void (async () => {
      const result = await fetchRoleUsage(organizationId, role.roleId);
      if (live) setUsage(result);
    })();
    return () => {
      live = false;
    };
  }, [organizationId, role]);

  const handleSave = async () => {
    if (!role) return;
    setSaving(true);

    try {
      const response = await apiFetch(
        `${rolesPath(organizationId)}/${role.roleId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            name: name.trim(),
            description: description.trim() || null,
            permissions,
          }),
        }
      );

      if (response.ok) {
        toast.success(t("common.namedSaved", name.trim()), {
          description: usage?.assignmentCount
            ? t(
                usage.assignmentCount === 1
                  ? "roles.oneAssignmentChanged"
                  : "roles.assignmentsChanged",
                usage.assignmentCount
              )
            : undefined,
        });
        fetchRole();
      } else {
        await reportApiError(response, {
          fallback: t("roles.failedSaveRole"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("roles.failedSaveRole"));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!role) return;

    try {
      const response = await apiFetch(
        `${rolesPath(organizationId)}/${role.roleId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedDeleted", role.name));
        setDeleteOpen(false);
        router.push(backHref);
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

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (notFound || !role) {
    return (
      <NotFound
        title={t("roles.roleNotFound")}
        description={t("common.notFound.description")}
        backHref={backHref}
        backLabel={t("roles.allRoles")}
      />
    );
  }

  const editable = canWrite && !role.isBuiltIn;
  const blocked = roleBlockedReason(name, description, permissions);
  const inUse = (usage?.assignmentCount ?? 0) > 0;
  const tabs = ["Permissions", ...(canReadHistory ? ["History"] : [])];

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("roles.role2")} title={role.name} description={role.description ?? undefined}>
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href={backHref}>
            <ArrowLeft className="size-4" />
            {t("roles.allRoles")}
          </Link>
        </Button>
        {editable && (
          <>
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={handleSave}
              disabled={saving || blocked !== null}
              title={blocked ?? undefined}
            >
              <Save className="size-4" />
              {saving ? "Saving..." : "Save"}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-critical-500 hover:text-critical-700"
              title={t("common.delete")}
              aria-label={t("common.delete")}
              onClick={() => setDeleteOpen(true)}
            >
              <Trash2 className="size-4" />
            </Button>
          </>
        )}
      </PageHeader>

      {role.isBuiltIn && (
        <div className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
          <p className="flex items-center gap-2 font-medium text-slate-700">
            <BuiltInBadge />
            {t("roles.thisRoleIsPartOf")}
          </p>
          <p className="mt-1.5">
            It is the same in every organisation and cannot be edited or deleted
            — the API answers 409 for either. What it carries is shown below; to
            get something close to it with a difference, compose a role of your
            own with these permissions as a starting point.
          </p>
        </div>
      )}

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.name")}
              </label>
              <Input
                value={name}
                onChange={(event) => setName(event.target.value)}
                disabled={!editable}
                maxLength={ROLE_NAME_MAX}
                className="font-mono"
              />
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.description")}
              </label>
              <Textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                disabled={!editable}
                maxLength={ROLE_DESCRIPTION_MAX}
                rows={2}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <FieldList>
            <Field label={t("common.kind")}>
              {role.isBuiltIn ? (
                <BuiltInBadge />
              ) : (
                <span className="font-mono text-xs text-slate-500">
                  composed by your organisation
                </span>
              )}
            </Field>
            <Field label={t("roles.heldBy")}>
              {usage ? (
                inUse ? (
                  <Link
                    href={assignmentsHref}
                    className="font-mono text-xs text-brand-800 hover:underline"
                  >
                    {usage.assignmentCount} assignment
                    {usage.assignmentCount === 1 ? "" : "s"} ·{" "}
                    {usage.userIds.length}{" "}
                    {usage.userIds.length === 1 ? "person" : "people"}
                  </Link>
                ) : (
                  <span className="font-mono text-xs text-slate-500">
                    nobody yet
                  </span>
                )
              ) : (
                <span className="font-mono text-xs text-slate-400">unknown</span>
              )}
            </Field>
            <Field label={t("common.created")} mono>
              <LocalTime value={role.createdAt} />
            </Field>
            <Field label={t("common.updated")} mono>
              <LocalTime value={role.updatedAt} />
            </Field>
            <Field label={t("roles.roleId")} mono>
              <span className="break-all text-xs text-slate-500">
                {role.roleId}
              </span>
            </Field>
          </FieldList>
        </CardContent>
      </Card>

      <div className="space-y-4">
        <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

        {activeTab === "Permissions" && (
          <div className="space-y-3">
            {editable ? (
              <>
                <p className="text-sm text-slate-500">
                  What is ticked here is what the role carries — saving states
                  the set whole, so unticking something removes it.
                  {inUse &&
                    ` Everybody on this role is affected the moment you save; ${usage?.assignmentCount} assignment${
                      usage?.assignmentCount === 1 ? "" : "s"
                    } carry it right now.`}
                </p>
                <PermissionPicker
                  selected={permissions}
                  onChange={setPermissions}
                  disabled={saving}
                />
              </>
            ) : (
              <>
                <p className="text-sm text-slate-500">
                  {role.isBuiltIn
                    ? "What this built-in role carries, in full."
                    : "What this role carries. Changing it needs roles:write."}
                </p>
                <Card>
                  <CardContent className="flex flex-wrap gap-x-4 gap-y-2 p-5">
                    {role.permissions.length === 0 ? (
                      <p className="text-sm text-slate-400">
                        Nothing — which the API does not allow on a composed
                        role, so this is a built-in with an empty bundle.
                      </p>
                    ) : (
                      role.permissions.map((permission) => (
                        <PermissionName key={permission} name={permission} />
                      ))
                    )}
                  </CardContent>
                </Card>
              </>
            )}
          </div>
        )}

        {activeTab === "History" && canReadHistory && (
          <HistoryPanel
            entityType="Role"
            path={historyPath.Role(organizationId, role.roleId)}
            entityId={role.roleId}
            onUnauthorized={onUnauthorized}
          />
        )}
      </div>

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
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
                    } would be left pointing at nothing, so the API refuses. Revoke them first.`
                  : `Nobody holds ${role.name}, so it can be removed outright.`
                : "Whether anybody still holds this role could not be read, so deleting may be refused."}
            </DialogDescription>
          </DialogHeader>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteOpen(false)}>
              {t("common.cancel")}
            </Button>
            {inUse ? (
              <Button asChild>
                <Link href={assignmentsHref}>{t("roles.revokeTheseFirst")}</Link>
              </Button>
            ) : (
              <Button variant="destructive" onClick={handleDelete}>
                {t("common.delete")}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
