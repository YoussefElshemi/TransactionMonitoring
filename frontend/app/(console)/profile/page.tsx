"use client";

import {
  ApplicationUser,
  ProblemDetails,
  UserResource,
  ValidationProblemDetails
} from "@/types";
import { Button } from "@/components/ui/button";
import { NotFound } from "@/components/NotFound";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/PageHeader";
import { MyPermissionsPanel } from "@/components/permissions/MyPermissionsPanel";
import { LanguageRegionPanel } from "@/components/preferences/LanguageRegionPanel";
import { FieldList, Field } from "@/components/FieldList";
import { StatusBadge } from "@/components/StatusBadge";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { loginHref } from "@/lib/authRedirect";
import { useTranslation } from "@/context/LocalizationContext";
import { API_BASE, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";

const Profile = () => {
  const t = useTranslation();
  const [loading, setLoading] = useState<boolean>(true);
  const [saveLoading, setSaveLoading] = useState<boolean>(false);
  const [user, setUser] = useState<ApplicationUser | null>(null);
  const [email, setEmail] = useState<string>("");
  const [oldPassword, setOldPassword] = useState<string>("");
  const [newPassword, setNewPassword] = useState<string>("");

  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const fetchUser = async () => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/auth/manage/info`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (response.ok) {
        const { user, organization } = await unwrap<UserResource>(response);
        setUser({ ...user, organization });
        setEmail(user.email);
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

  const handleSave = async () => {
    if (!user) return;

    setSaveLoading(true);

    const payload: { [key: string]: string } = {};
    if (email !== user.email) {
      payload.newEmail = email;
    }

    if (oldPassword.length) {
      payload.oldPassword = oldPassword;
    }

    if (newPassword.length) {
      payload.newPassword = newPassword;
    }

    try {
      const response = await apiFetch(`${API_BASE}/auth/manage/info`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include",
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        toast.success(t("profile.successfullyUpdatedUser"));
        fetchUser();
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
          errorData.detail || t("common.errorOccurredSavingChanges")
        );
      }
    } catch {
      toast.error(t("common.errorOccurredSavingChanges"));
    } finally {
      setSaveLoading(false);
    }
  };

  useEffect(() => {
    fetchUser();
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-32" />
        <div className="grid gap-6 lg:grid-cols-2">
          <Card className="flex flex-col">
            <CardContent className="flex-1 space-y-4 pt-6">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-9 w-full" />
              ))}
            </CardContent>
            <CardFooter className="justify-end border-t border-slate-200 pt-6">
              <Skeleton className="h-10 w-32" />
            </CardFooter>
          </Card>
          <Card>
            <CardContent className="space-y-4 pt-6">
              {Array.from({ length: 6 }).map((_, i) => (
                <Skeleton key={i} className="h-9 w-full" />
              ))}
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  if (!user) {
    return <NotFound
        title={t("profile.profileUnavailable")}
        description={t("profile.weCouldnTLoadYour")}
      />;
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("profile.account2")}
        title={t("profile.profile")}
        description={t("profile.yourSignInDetailsAnd")}
      />

      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="flex flex-col">
          <CardHeader>
            <CardTitle>{t("profile.account")}</CardTitle>
          </CardHeader>
          <CardContent className="flex-1 space-y-4">
            <div className="space-y-1.5">
              <label
                htmlFor="email"
                className="block font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("common.email")}
              </label>
              <Input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <label
                htmlFor="oldPassword"
                className="block font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("profile.currentPassword")}
              </label>
              <Input
                id="oldPassword"
                type="password"
                value={oldPassword}
                onChange={(e) => setOldPassword(e.target.value)}
                autoComplete="current-password"
              />
            </div>
            <div className="space-y-1.5">
              <label
                htmlFor="newPassword"
                className="block font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("profile.newPassword")}
              </label>
              <Input
                id="newPassword"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                autoComplete="new-password"
              />
              <p className="text-xs text-slate-400">
                {t("profile.leaveBothPasswordFieldsEmpty")}
              </p>
            </div>
          </CardContent>
          <CardFooter className="justify-end border-t border-slate-200 pt-6">
            <Button onClick={handleSave} disabled={saveLoading}>
              {saveLoading ? "Saving…" : "Save changes"}
            </Button>
          </CardFooter>
        </Card>

        <Card className="divide-y divide-slate-200">
          <section>
            <h2 className="px-6 pb-1 pt-6 font-mono text-base font-medium leading-none tracking-tight">
              {t("common.details")}
            </h2>
            <FieldList>
              <Field label={t("common.username")} mono>
                {user.userName}
              </Field>
              <Field label={t("profile.builtInRoles")} mono>
                {user.roles?.length ? user.roles.join(", ") : "—"}
              </Field>
              <Field label={t("profile.emailConfirmed")} mono>
                {user.isEmailConfirmed ? "yes" : "no"}
              </Field>
              <Field label={t("common.created")} mono>
                <LocalTime value={user.createdAt} />
              </Field>
              <Field label={t("common.updated")} mono>
                <LocalTime value={user.updatedAt} />
              </Field>
            </FieldList>
          </section>

          {user.organization && (
            <section>
              <h2 className="px-6 pb-1 pt-6 font-mono text-base font-medium leading-none tracking-tight">
                {t("common.organization")}
              </h2>
              <FieldList>
                <Field label={t("common.name")}>{user.organization.friendlyName}</Field>
                <Field label={t("common.status")}>
                  <StatusBadge status={user.organization.status} />
                </Field>
                <Field label={t("common.created")} mono>
                  <LocalTime value={user.organization.createdAt} />
                </Field>
                <Field label={t("common.updated")} mono>
                  <LocalTime value={user.organization.updatedAt} />
                </Field>
              </FieldList>
            </section>
          )}
        </Card>
      </div>

      <section aria-labelledby="language-region">
        <h2
          id="language-region"
          className="mb-3 font-mono text-base font-medium leading-none tracking-tight text-slate-900"
        >
          {t("preferences.title")}
        </h2>
        <div className="lg:max-w-2xl">
          <LanguageRegionPanel
            onUnauthorized={() => router.push(loginHref(redirectUri))}
          />
        </div>
      </section>

      <MyPermissionsPanel />
    </div>
  );
};

export default Profile;
