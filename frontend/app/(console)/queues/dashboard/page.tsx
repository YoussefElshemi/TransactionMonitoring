"use client";

import { useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { QueueDashboard } from "@/components/queues/QueueDashboard";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function QueueDashboardPage() {
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
    <QueueDashboard
      organizationId={user.organizationId}
      queueHref={(queueId) => `/queues/manage/${queueId}`}
      manageHref="/queues/manage"
      unroutedHref="/cases?unrouted=true&status="
      onUnauthorized={goToLogin}
    />
  );
}
