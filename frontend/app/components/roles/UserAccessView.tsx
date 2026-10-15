"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { ApplicationUser, UserResource } from "@/types";
import { API_BASE, unwrap } from "@/lib/api";
import { userName } from "@/lib/resources";
import { usePermissions } from "@/context/PermissionsContext";
import { PageHeader } from "@/components/PageHeader";
import { RoleAssignmentsPanel } from "@/components/roles/RoleAssignmentsPanel";
import { HomeSubOrganizationControl } from "@/components/subOrganizations/HomeSubOrganizationControl";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function UserAccessView({
  organizationId,
  userId,
  backHref,
  roleHref,
  onUnauthorized,
}: {
  organizationId: string;
  userId: string;
  backHref: string;
  roleHref: (roleId: string) => string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();

  const [subject, setSubject] = useState<ApplicationUser | null>(null);
  const [loadingSubject, setLoadingSubject] = useState(true);

  const canReadUsers = holds("users:read");

  useEffect(() => {
    if (!canReadUsers) {
      setLoadingSubject(false);
      return;
    }

    let live = true;

    void (async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/users/${userId}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );
        if (!live) return;
        if (response.ok) {
          const resource = await unwrap<UserResource>(response);
          setSubject(resource.user);
        }
      } catch {
      } finally {
        if (live) setLoadingSubject(false);
      }
    })();

    return () => {
      live = false;
    };
  }, [canReadUsers, organizationId, userId]);

  const displayName = subject ? (userName(subject) ?? subject.userName) : userId;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("organization.access")}
        title={loadingSubject ? "…" : displayName}
        description={t("organization.everythingThatDecidesWhatThis")}
      >
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href={backHref}>
            <ArrowLeft className="size-4" />
            {t("organization.allGrants")}
          </Link>
        </Button>
      </PageHeader>

      {loadingSubject ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        subject && (
          <HomeSubOrganizationControl
            organizationId={organizationId}
            userId={userId}
            current={subject.subOrganizationId}
            onChanged={(subOrganizationId) =>
              setSubject((current) =>
                current ? { ...current, subOrganizationId } : current
              )
            }
            onUnauthorized={onUnauthorized}
          />
        )
      )}

      <RoleAssignmentsPanel
        organizationId={organizationId}
        userId={userId}
        roleHref={roleHref}
        embedded
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
