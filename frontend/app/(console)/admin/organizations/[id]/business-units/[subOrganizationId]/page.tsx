"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { SubOrganizationDetail } from "@/components/subOrganizations/SubOrganizationDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function AdminOrganizationBusinessUnit() {
  const { id, subOrganizationId } = useParams();
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
    ["sub-organizations:read", "platform:administer"],
    "/"
  );

  if (!allowed) return null;

  return (
    <SubOrganizationDetail
      organizationId={id as string}
      subOrganizationId={subOrganizationId as string}
      backHref={`/admin/organizations/${id}#Business%20Units`}
      onUnauthorized={goToLogin}
    />
  );
}
