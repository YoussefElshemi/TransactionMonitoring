"use client";

import { useParams } from "next/navigation";

import { WebhookConfigurationDetail } from "@/components/webhooks/WebhookConfigurationDetail";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationWebhook() {
  const { webhookConfigurationId } = useParams();
  const { allowed } = useRequirePermission(
    "webhook-configurations:read",
    "/organization"
  );

  if (!allowed) return null;

  return (
    <WebhookConfigurationDetail
      organizationSegment="my"
      webhookConfigurationId={webhookConfigurationId as string}
      deliveriesHref={(id) =>
        `/organization/webhooks/deliveries?webhookConfigurationId=${id}`
      }
    />
  );
}
