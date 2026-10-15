"use client";

import { useCallback } from "react";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams,
} from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { EscalationRuleDetail } from "@/components/escalations/EscalationRuleDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationEscalationRule() {
  const { escalationRuleId } = useParams();
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
    "escalation-rules:read",
    "/organization"
  );

  if (!allowed || !user) return null;

  return (
    <EscalationRuleDetail
      organizationId={user.organizationId}
      escalationRuleId={escalationRuleId as string}
      backHref="/organization#Escalation%20Rules"
      onUnauthorized={goToLogin}
    />
  );
}
