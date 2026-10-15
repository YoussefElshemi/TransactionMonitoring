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
import { KeyRound, UserX, History } from "lucide-react";
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
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { relationText } from "@/lib/resources";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const Profile = () => {
  const t = useTranslation();
  const [loading, setLoading] = useState<boolean>(true);
  const [resource, setResource] = useState<UserResource | null>(null);
  const user = resource?.user ?? null;
  const [historyOpen, setHistoryOpen] = useState<boolean>(false);

  const { id, userId } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchUser = async (organizationId: string, userId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

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

  const removeUser = async (organizationId: string, userId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyRemovedUser"));
        router.push(`/admin/organizations/${organizationId}`);
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
        toast.error(errorData.detail || t("organizations.failedDeleteUser"));
      }
    } catch {
      toast.error(t("organizations.failedDeleteUser"));
    }
  };

  useEffect(() => {
    fetchUser(id as string, userId as string);
  }, []);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="pt-6 space-y-3">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!user) {
    return <NotFound
        title={t("organizations.userNotFound")}
        description={t("organizations.thisAccountMayHaveBeen")}
      />;
  }

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("organizations.organizationUser")} title={user.userName}>
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() =>
            router.push(
              `/admin/organizations/${id}/users/${user.userId}/access`
            )
          }
        >
          <KeyRound className="size-4" />
          {t("organization.access")}
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => setHistoryOpen(true)}
        >
          <History className="size-4" />
          {t("common.history")}
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 text-critical-500 hover:text-critical-700"
          title={t("organizations.removeUser")}
          onClick={() => removeUser(id as string, user.userId)}
        >
          <UserX className="size-4" />
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
              <span className="text-sm font-medium text-slate-500">{t("organizations.emailAddress")}</span>
              <p className="mt-0.5 text-sm text-slate-900">{user.email}</p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("common.roles")}</span>
              <div className="mt-1 flex flex-wrap gap-1.5">
                {user.roles.length > 0 ? (
                  user.roles.map((role) => (
                    <Badge key={role} variant="outline" className="font-medium">
                      {role}
                    </Badge>
                  ))
                ) : (
                  <span className="text-sm text-slate-400">{t("organizations.noRolesAssigned")}</span>
                )}
              </div>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("organizations.emailConfirmed")}</span>
              <div className="mt-1">
                <StatusBadge status={user.isEmailConfirmed ? "Enabled" : "Disabled"} />
              </div>
            </div>
            {user.organizationId && (
              <div>
                <span className="text-sm font-medium text-slate-500">{t("common.organization")}</span>
                <p className="mt-0.5 text-sm text-slate-900">
                  {relationText(
                    resource?.organization?.friendlyName,
                    user.organizationId
                  )}
                </p>
              </div>
            )}
            <div>
              <span className="text-sm font-medium text-slate-500">{t("organizations.createdAt")}</span>
              <p className="mt-0.5 text-sm text-slate-900">
                <LocalTime value={user.createdAt} />
              </p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">{t("organizations.updatedAt")}</span>
              <p className="mt-0.5 text-sm text-slate-900">
                <LocalTime value={user.updatedAt} />
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <HistoryDialog
        open={historyOpen}
        onOpenChange={setHistoryOpen}
        title={user.userName}
        entityType="ApplicationUser"
        path={historyPath.ApplicationUser(id as string, user.userId)}
        entityId={user.userId}
        onUnauthorized={goToLogin}
        scopeNote={USER_HISTORY_SCOPE_NOTE}
      />
    </div>
  );
};

export default Profile;
