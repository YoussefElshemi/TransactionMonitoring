"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function MyOrganizationReasonCodesRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/organization#Reason%20Codes");
  }, [router]);

  return null;
}
