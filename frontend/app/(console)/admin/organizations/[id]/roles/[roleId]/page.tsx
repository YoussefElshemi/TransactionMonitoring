"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { RoleDetail } from "@/components/roles/RoleDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function AdminOrganizationRole() {
  const { id, roleId } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const { allowed } = useRequirePermission(
    ["roles:read", "platform:administer"],
    "/"
  );

  if (!allowed) return null;

  return (
    <RoleDetail
      organizationId={id as string}
      roleId={roleId as string}
      backHref={`/admin/organizations/${id}#Roles`}
      assignmentsHref={`/admin/organizations/${id}#Access`}
      onUnauthorized={goToLogin}
    />
  );
}
