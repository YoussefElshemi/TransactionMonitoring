"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { UserAccessView } from "@/components/roles/UserAccessView";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function AdminOrganizationUserAccess() {
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

  const { allowed } = useRequirePermission(
    ["roles:read", "platform:administer"],
    "/"
  );

  if (!allowed) return null;

  return (
    <UserAccessView
      organizationId={id as string}
      userId={userId as string}
      backHref={`/admin/organizations/${id}#Access`}
      roleHref={(roleId) => `/admin/organizations/${id}/roles/${roleId}`}
      onUnauthorized={goToLogin}
    />
  );
}
