"use client";

import { useParams } from "next/navigation";

import { ApiClientDetail } from "@/components/apiClients/ApiClientDetail";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function MyOrganizationApiClient() {
  const { apiClientId } = useParams();
  const { allowed } = useRequirePermission("api-clients:read", "/organization");

  if (!allowed) return null;

  return <ApiClientDetail apiClientId={apiClientId as string} />;
}
