"use client";

import { useCallback } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";

import { WebhookDeliveriesPanel } from "@/components/webhooks/WebhookDeliveriesPanel";
import { usePermissions } from "@/context/PermissionsContext";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationWebhookDeliveries() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds } = usePermissions();

  const { allowed } = useRequirePermission(
    "webhook-configurations:read",
    "/organization"
  );

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  if (!allowed) return null;

  return (
    <WebhookDeliveriesPanel
      organizationSegment="my"
      canWrite={holds("webhook-configurations:write")}
      initialWebhookConfigurationId={
        searchParams.get("webhookConfigurationId") ?? undefined
      }
      configurationHref={(webhookConfigurationId) =>
        `/organization/webhooks/${webhookConfigurationId}`
      }
      onUnauthorized={goToLogin}
    />
  );
}
