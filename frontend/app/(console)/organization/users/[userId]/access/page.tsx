"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { UserAccessView } from "@/components/roles/UserAccessView";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function UserAccess() {
  const { userId } = useParams();
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
    <UserAccessView
      organizationId={user.organizationId}
      userId={userId as string}
      backHref="/organization#Access"
      roleHref={(roleId) => `/organization/roles/${roleId}`}
      onUnauthorized={goToLogin}
    />
  );
}
