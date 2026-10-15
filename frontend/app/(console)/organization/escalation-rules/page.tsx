"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function MyOrganizationEscalationRulesRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/organization#Escalation%20Rules");
  }, [router]);

  return null;
}
