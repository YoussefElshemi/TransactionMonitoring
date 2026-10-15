"use client";

import { useCallback } from "react";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams
} from "next/navigation";

import { WebhookDeliveriesPanel } from "@/components/webhooks/WebhookDeliveriesPanel";
import { usePermissions } from "@/context/PermissionsContext";
import { loginHref } from "@/lib/authRedirect";

const OrganizationWebhookDeliveries = () => {
  const { id } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  return (
    <WebhookDeliveriesPanel
      organizationSegment={id as string}
      canWrite={holds("webhook-configurations:write")}
      initialWebhookConfigurationId={
        searchParams.get("webhookConfigurationId") ?? undefined
      }
      configurationHref={(webhookConfigurationId) =>
        `/admin/organizations/${id}/webhook-configurations/${webhookConfigurationId}`
      }
      onUnauthorized={goToLogin}
    />
  );
};

export default OrganizationWebhookDeliveries;
