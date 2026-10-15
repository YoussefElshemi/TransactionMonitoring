"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { ReasonCodeDetail } from "@/components/reasonCodes/ReasonCodeDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function AdminOrganizationReasonCode() {
  const { id, caseReasonCodeId } = useParams();
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
    ["case-reason-codes:read", "platform:administer"],
    "/"
  );

  if (!allowed) return null;

  return (
    <ReasonCodeDetail
      organizationId={id as string}
      caseReasonCodeId={caseReasonCodeId as string}
      backHref={`/admin/organizations/${id}#Reason%20Codes`}
      onUnauthorized={goToLogin}
    />
  );
}
