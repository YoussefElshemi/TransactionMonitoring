"use client";

import { Filter } from "lucide-react";

import { Permission } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { cn } from "@/lib/utils";

export function NarrowedListingNote({
  permission,
  noun,
  className,
}: {
  permission: Permission;
  noun: string;
  className?: string;
}) {
  const { holds, organizationWide, resolved } = usePermissions();

  if (!resolved) return null;
  if (!holds(permission)) return null;
  if (organizationWide(permission)) return null;

  return (
    <p
      className={cn(
        "flex items-center gap-2 text-xs text-slate-500",
        className
      )}
    >
      <Filter className="size-3.5 flex-shrink-0 text-slate-400" />
      This is the part of the organisation your access reaches, counts included
      — not every {noun} it has.
    </p>
  );
}
