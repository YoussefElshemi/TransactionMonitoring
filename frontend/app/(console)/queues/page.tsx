"use client";

import { useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { MyQueuesPanel } from "@/components/queues/MyQueuesPanel";
import { loginHref } from "@/lib/authRedirect";

export default function MyQueues() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  return (
    <MyQueuesPanel
      caseHref={(caseId) => `/cases/${caseId}`}
      myCasesHref="/my-cases"
      onUnauthorized={goToLogin}
    />
  );
}
