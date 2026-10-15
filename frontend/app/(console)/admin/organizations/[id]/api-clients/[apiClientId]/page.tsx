"use client";

import { useParams } from "next/navigation";

import { ApiClientDetail } from "@/components/apiClients/ApiClientDetail";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function AdminOrganizationApiClient() {
  const { id, apiClientId } = useParams();
  const { allowed } = useRequirePermission(
    ["api-clients:read", "platform:administer"],
    "/"
  );

  if (!allowed) return null;

  return (
    <ApiClientDetail
      apiClientId={apiClientId as string}
      organizationId={id as string}
      listHref={`/admin/organizations/${id}#API%20Clients`}
      detailHref={(clientId) =>
        `/admin/organizations/${id}/api-clients/${clientId}`
      }
    />
  );
}
