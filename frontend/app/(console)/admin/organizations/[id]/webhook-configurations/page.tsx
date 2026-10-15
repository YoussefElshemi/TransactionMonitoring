"use client";

import { useParams } from "next/navigation";

import { WebhookConfigurationsPanel } from "@/components/webhooks/WebhookConfigurationsPanel";

const OrganizationWebhookConfigurations = () => {
  const { id } = useParams();

  return (
    <WebhookConfigurationsPanel
      organizationSegment={id as string}
      detailHref={(webhookConfigurationId) =>
        `/admin/organizations/${id}/webhook-configurations/${webhookConfigurationId}`
      }
    />
  );
};

export default OrganizationWebhookConfigurations;
