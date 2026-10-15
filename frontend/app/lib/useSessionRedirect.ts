"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";

export const useSessionRedirect = (redirectUri: string): boolean => {
  const { user, refreshUser } = useAuth();
  const router = useRouter();
  const [confirmed, setConfirmed] = useState(false);
  const [inheritedSession] = useState(() => Boolean(user));

  useEffect(() => {
    let current = true;
    void refreshUser().finally(() => {
      if (current) setConfirmed(true);
    });
    return () => {
      current = false;
    };
    // Once per visit. `refreshUser` is rebuilt on every provider render, so as
    // a dependency it would restart the check on its own result, forever.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!confirmed || !user) return;
    router.replace(redirectUri);
  }, [confirmed, user, router, redirectUri]);

  return !user && (confirmed || !inheritedSession);
};
