"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { RoleDetail } from "@/components/roles/RoleDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationRole() {
  const { roleId } = useParams();
  const { user } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const { allowed } = useRequirePermission("roles:read", "/organization");

  if (!allowed || !user) return null;

  return (
    <RoleDetail
      organizationId={user.organizationId}
      roleId={roleId as string}
      backHref="/organization#Roles"
      assignmentsHref="/organization#Access"
      onUnauthorized={goToLogin}
    />
  );
}
