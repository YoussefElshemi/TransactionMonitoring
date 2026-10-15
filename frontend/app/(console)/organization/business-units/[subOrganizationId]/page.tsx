"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { SubOrganizationDetail } from "@/components/subOrganizations/SubOrganizationDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationBusinessUnit() {
  const { subOrganizationId } = useParams();
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

  const { allowed } = useRequirePermission(
    "sub-organizations:read",
    "/organization"
  );

  if (!allowed || !user) return null;

  return (
    <SubOrganizationDetail
      organizationId={user.organizationId}
      subOrganizationId={subOrganizationId as string}
      backHref="/organization#Business%20Units"
      onUnauthorized={goToLogin}
    />
  );
}
