"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function MyOrganizationWebhooksRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/organization#Webhooks");
  }, [router]);

  return null;
}
