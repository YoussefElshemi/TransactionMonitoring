"use client";

import { useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { QueuesPanel } from "@/components/queues/QueuesPanel";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function ManageQueues() {
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

  const { allowed } = useRequirePermission("queues:read", "/queues");

  if (!allowed || !user) return null;

  return (
    <QueuesPanel
      organizationId={user.organizationId}
      detailHref={(queueId) => `/queues/manage/${queueId}`}
      dashboardHref="/queues/dashboard"
      onUnauthorized={goToLogin}
    />
  );
}
