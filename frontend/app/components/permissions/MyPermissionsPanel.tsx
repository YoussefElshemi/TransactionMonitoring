"use client";

import { useMemo } from "react";
import { TriangleAlert } from "lucide-react";

import { PermissionGrant } from "@/types";
import { resourceLabel, splitPermission } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { PermissionName, ScopeBadge } from "@/components/roles/RoleParts";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useTranslation } from "@/context/LocalizationContext";

export function MyPermissionsPanel() {
  const t = useTranslation();
  const { grants, resolved, failed } = usePermissions();

  const groups = useMemo(() => {
    const byResource = new Map<string, PermissionGrant[]>();

    for (const grant of grants ?? []) {
      const { resource } = splitPermission(grant.name);
      const existing = byResource.get(resource);
      if (existing) existing.push(grant);
      else byResource.set(resource, [grant]);
    }

    return Array.from(byResource.entries())
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([resource, entries]) => ({
        resource,
        label: resourceLabel(resource),
        entries: entries.sort((a, b) => a.name.localeCompare(b.name)),
      }));
  }, [grants]);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("permissions.whatYouCanDo")}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-slate-500">
          Every permission granted to you, and how far each one reaches. This is
          the list the app checks — your roles are only how you came to have it.
        </p>

        {!resolved ? (
          <div className="space-y-2">
            {Array.from({ length: 5 }).map((_, index) => (
              <Skeleton key={index} className="h-5 w-full" />
            ))}
          </div>
        ) : failed ? (
          <div className="flex gap-2.5 rounded-md border border-attention-200 bg-attention-50 px-3 py-2.5 ring-1 ring-inset ring-attention-200">
            <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <p className="text-sm text-slate-600">
              Your permissions could not be read, so this list is not a statement
              that you hold none. Screens elsewhere in the app will be hiding
              controls you may in fact have; a reload usually fixes it.
            </p>
          </div>
        ) : groups.length === 0 ? (
          <p className="text-sm text-slate-500">
            No permissions have been granted to you yet. You can sign in and see
            your own account, and nothing else — somebody with{" "}
            <span className="font-mono">roles:assign</span> in your organisation
            can grant you a role.
          </p>
        ) : (
          <div className="space-y-4">
            {groups.map((group) => (
              <div key={group.resource}>
                <p className="mb-1.5 font-mono text-[11px] uppercase tracking-wider text-slate-400">
                  {group.label}
                </p>
                <ul className="space-y-1">
                  {group.entries.map((grant, index) => (
                    <li
                      key={`${grant.name}-${index}`}
                      className="flex flex-wrap items-center gap-2"
                    >
                      <PermissionName name={grant.name} />
                      <ScopeBadge scope={grant.scope} />
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
