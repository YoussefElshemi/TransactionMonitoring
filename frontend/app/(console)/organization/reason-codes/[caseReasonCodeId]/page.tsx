"use client";

import { useCallback } from "react";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { ReasonCodeDetail } from "@/components/reasonCodes/ReasonCodeDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationReasonCode() {
  const { caseReasonCodeId } = useParams();
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
    "case-reason-codes:read",
    "/organization"
  );

  if (!allowed || !user) return null;

  return (
    <ReasonCodeDetail
      organizationId={user.organizationId}
      caseReasonCodeId={caseReasonCodeId as string}
      backHref="/organization#Reason%20Codes"
      onUnauthorized={goToLogin}
    />
  );
}
