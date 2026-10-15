"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

import { usePermissions } from "@/context/PermissionsContext";
import { Permission, ScopeTarget } from "@/lib/permissions";

export const useRequirePermission = (
  permissions: Permission | Permission[],
  fallbackHref: string,
  target?: ScopeTarget
): { allowed: boolean; resolved: boolean } => {
  const router = useRouter();
  const { holds, holdsOver, resolved } = usePermissions();

  const required = Array.isArray(permissions) ? permissions : [permissions];
  const allowed = required.every((permission) =>
    target ? holdsOver(permission, target) : holds(permission)
  );

  // Both are fresh values each render, so the effect keys off their contents
  // rather than their identity.
  const key = required.join(",");
  const targetKey = target ? JSON.stringify(target) : "";

  useEffect(() => {
    if (resolved && !allowed) router.replace(fallbackHref);
  }, [resolved, allowed, router, fallbackHref, key, targetKey]);

  return { allowed, resolved };
};
