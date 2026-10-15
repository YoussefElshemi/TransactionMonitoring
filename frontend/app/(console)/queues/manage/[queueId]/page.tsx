"use client";

import { useCallback } from "react";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams,
} from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { QueueDetail } from "@/components/queues/QueueDetail";
import { loginHref } from "@/lib/authRedirect";
import { useRequirePermission } from "@/lib/useRequirePermission";

export default function ManageQueue() {
  const { queueId } = useParams();
  const { user } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const { allowed } = useRequirePermission("queues:read", "/queues", {
    queueId: queueId as string,
  });

  if (!allowed || !user) return null;

  return (
    <QueueDetail
      organizationId={user.organizationId}
      queueId={queueId as string}
      backHref="/queues/manage"
      onUnauthorized={goToLogin}
    />
  );
}
