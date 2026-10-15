"use client";

import { useParams } from "next/navigation";

import { WebhookConfigurationDetail } from "@/components/webhooks/WebhookConfigurationDetail";

const OrganizationWebhookConfiguration = () => {
  const { id, webhookConfigurationId } = useParams();

  return (
    <WebhookConfigurationDetail
      organizationSegment={id as string}
      webhookConfigurationId={webhookConfigurationId as string}
      historyOrganizationId={id as string}
      deliveriesHref={(webhookConfigurationId) =>
        `/admin/organizations/${id}/webhook-deliveries?webhookConfigurationId=${webhookConfigurationId}`
      }
    />
  );
};

export default OrganizationWebhookConfiguration;
