"use client";

import {
  UserResource,
  ProblemDetails,
  ValidationProblemDetails
} from "@/types";
import {
  useRouter,
  usePathname,
  useSearchParams,
  useParams
} from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Trash2, History } from "lucide-react";
import { toast } from "sonner";
import {
  HistoryDialog,
  historyPath,
  USER_HISTORY_SCOPE_NOTE,
} from "@/components/history";
import { PageHeader } from "@/components/PageHeader";
import { NotFound } from "@/components/NotFound";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { relationText } from "@/lib/resources";
import { HomeUnitCell } from "@/components/subOrganizations/SubOrganizationParts";
import { useRequirePermission } from "@/lib/useRequirePermission";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const AVAILABLE_ROLES = ["User", "Manager", "Admin", "Auditor"] as const;

const Profile = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission("platform:administer", "/");

  const [loading, setLoading] = useState<boolean>(true);
  const [resource, setResource] = useState<UserResource | null>(null);
  const user = resource?.user ?? null;
  const organizationName = relationText(
    resource?.organization?.friendlyName,
    user?.organizationId
  );
  const [historyOpen, setHistoryOpen] = useState<boolean>(false);

  const { id } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchUser = async (userId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/users/${userId}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (response.ok) {
        setResource(await unwrap<UserResource>(response));
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), {
          id: "unauthorized"
        });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission"),
          {
            id: "forbidden"
          }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.errorOccurredFetchingUser"),
          { id: "error-fetching" }
        );
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingUser"), {
        id: "error-fetching"
      });
    } finally {
      setLoading(false);
    }
  };

  const deleteUser = async (userId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/users/${userId}`, {
        method: "DELETE",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (response.ok) {
        toast.success(t("users.successfullyDeletedUser"), {
          action: { label: t("common.undo"), onClick: () => recoverUser(userId) },
        });
        router.push("/admin/users");
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("users.errorOccurredDeletingUser"),
          { id: "error-deleting" }
        );
      }
    } catch {
      toast.error(t("users.errorOccurredDeletingUser"), {
        id: "error-deleting"
      });
    } finally {
      setLoading(false);
    }
  };

  const recoverUser = async (userId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users/${userId}/recover`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("users.successfullyRecoveredUser"));
        router.push(`/admin/users/${userId}`);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("users.errorOccurredDeletingUser"),
          { id: "error-deleting" }
        );
      }
    } catch {
      toast.error(t("users.errorOccurredDeletingUser"));
    } finally {
      setLoading(false);
    }
  };

  const addRole = async (userId: string, role: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users/${userId}/roles/${role}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("users.successfullyAddedRole"));
        fetchUser(userId);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("users.errorOccurredAddingRole"), {
          id: "error-adding"
        });
      }
    } catch {
      toast.error(t("users.errorOccurredAddingRole"), {
        id: "error-adding"
      });
    } finally {
      setLoading(false);
    }
  };

  const removeRole = async (userId: string, role: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users/${userId}/roles/${role}`,
        {
          method: "DELETE",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("users.successfullyRemovedRole"));
        fetchUser(userId);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("users.errorOccurredRemovingRole")
        );
      }
    } catch {
      toast.error(t("users.errorOccurredRemovingRole"));
    } finally {
      setLoading(false);
    }
  };

  const handleRoleChange = (userId: string, role: string, add: boolean) => {
    if (add) {
      addRole(userId, role);
    } else {
      removeRole(userId, role);
    }
  };

  useEffect(() => {
    if (!allowed) return;
    fetchUser(id as string);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allowed, id]);

  if (!allowed) return null;

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="pt-6 space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!user) {
    return <NotFound
        title={t("users.userNotFound")}
        description={t("users.thisAccountMayHaveBeen")}
        backHref="/admin/users"
        backLabel={t("users.allUsers")}
      />;
  }

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("users.user")} title={user.userName}>
        {user.organizationId && (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() => setHistoryOpen(true)}
          >
            <History className="size-4" />
            {t("common.history")}
          </Button>
        )}
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 text-critical-500 hover:text-critical-700"
          onClick={() => deleteUser(id as string)}
        >
          <Trash2 className="size-4" />
        </Button>
      </PageHeader>

      <Card>
        <CardContent className="pt-6">
          <div className="grid grid-cols-2 gap-x-8 gap-y-4">
            <div>
              <span className="text-sm font-medium text-slate-500">{t("common.username")}</span>
              <p className="mt-0.5 text-sm text-slate-900">{user.userName}</p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("users.emailAddress")}</span>
              <p className="mt-0.5 text-sm text-slate-900">{user.email}</p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("users.emailConfirmed")}</span>
              <div className="mt-1">
                <StatusBadge status={user.isEmailConfirmed ? "Enabled" : "Disabled"} />
              </div>
            </div>
            {user.organizationId && (
              <div>
                <span className="text-sm font-medium text-slate-500">{t("common.organization")}</span>
                <p className="mt-0.5 text-sm text-slate-900">
                  {organizationName}
                </p>
              </div>
            )}
            {user.organizationId && (
              <div>
                <span className="text-sm font-medium text-slate-500">
                  {t("users.businessUnit")}
                </span>
                <div className="mt-1">
                  <HomeUnitCell subOrganizationId={user.subOrganizationId} />
                </div>
                <p className="mt-1 text-xs text-slate-500">
                  {user.subOrganizationId
                    ? "Every organisation-wide permission this account holds is narrowed to that unit, and it holds no platform:administer. Changed from the organisation's Access screen."
                    : "Every permission this account holds reaches its whole organisation."}
                </p>
              </div>
            )}
            <div>
              <span className="text-sm font-medium text-slate-500">{t("users.currentRoles")}</span>
              <div className="mt-1 flex flex-wrap gap-1.5">
                {user.roles.length > 0 ? (
                  user.roles.map((role) => (
                    <Badge key={role} variant="outline" className="font-medium">
                      {role}
                    </Badge>
                  ))
                ) : (
                  <span className="text-sm text-slate-400">{t("users.noRolesAssigned")}</span>
                )}
              </div>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("users.createdAt")}</span>
              <p className="mt-0.5 text-sm text-slate-900">
                <LocalTime value={user.createdAt} />
              </p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("users.updatedAt")}</span>
              <p className="mt-0.5 text-sm text-slate-900">
                <LocalTime value={user.updatedAt} />
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t("users.roleAssignment")}</CardTitle>
        </CardHeader>
        <Separator />
        <CardContent className="pt-4">
          <div className="flex flex-col gap-3">
            {AVAILABLE_ROLES.map((role) => (
              <label
                key={role}
                className="flex cursor-pointer items-center gap-3 rounded-md border border-slate-200 px-4 py-3 hover:bg-slate-50"
              >
                <input
                  type="checkbox"
                  checked={user.roles.includes(role)}
                  onChange={(e) =>
                    handleRoleChange(user.userId, role, e.target.checked)
                  }
                  className="h-4 w-4 rounded border-slate-300 accent-brand-800"
                />
                <span className="text-sm font-medium text-slate-700">{role}</span>
              </label>
            ))}
          </div>
        </CardContent>
      </Card>

      {user.organizationId && (
        <HistoryDialog
          open={historyOpen}
          onOpenChange={setHistoryOpen}
          title={user.userName}
          entityType="ApplicationUser"
          path={historyPath.ApplicationUser(user.organizationId, user.userId)}
          entityId={user.userId}
          onUnauthorized={goToLogin}
          scopeNote={`${USER_HISTORY_SCOPE_NOTE} This trail is for ${organizationName}.`}
        />
      )}
    </div>
  );
};

export default Profile;
