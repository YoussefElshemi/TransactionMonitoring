"use client";

import { useCallback } from "react";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams,
} from "next/navigation";

import { EscalationRuleDetail } from "@/components/escalations/EscalationRuleDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function AdminOrganizationEscalationRule() {
  const { id, escalationRuleId } = useParams();
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
    ["escalation-rules:read", "platform:administer"],
    "/"
  );

  if (!allowed) return null;

  return (
    <EscalationRuleDetail
      organizationId={id as string}
      escalationRuleId={escalationRuleId as string}
      backHref={`/admin/organizations/${id}#Escalation%20Rules`}
      onUnauthorized={goToLogin}
    />
  );
}
