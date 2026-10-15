"use client";

import { Building2, Layers, Lock, Shield, Users } from "lucide-react";

import { PermissionScope, PermissionScopeType } from "@/types";
import {
  actionLabel,
  resourceLabel,
  SCOPE_TYPE_LABELS,
  scopeId,
  splitPermission,
} from "@/lib/permissions";
import { shortId } from "@/lib/resources";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function BuiltInBadge() {
  const t = useTranslation();
  return (
    <span
      className="inline-flex items-center gap-1 rounded border border-slate-200 bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wider text-slate-500"
      title={t("roles.builtInToTheProduct")}
    >
      <Lock className="size-3" />
      built in
    </span>
  );
}

export function PermissionName({
  name,
  className,
}: {
  name: string;
  className?: string;
}) {
  const { resource, action } = splitPermission(name);
  return (
    <span
      className={cn("font-mono text-xs text-slate-700", className)}
      title={name}
    >
      <span className="text-slate-500">{resourceLabel(resource)}</span>
      <span className="px-1 text-slate-300">·</span>
      {actionLabel(action)}
    </span>
  );
}

const SCOPE_ICONS: Record<PermissionScopeType, typeof Building2> = {
  Organization: Building2,
  SubOrganization: Users,
  RuleSet: Shield,
  Queue: Layers,
};

export function ScopeBadge({
  scope,
  name,
  className,
}: {
  scope: PermissionScope;
  name?: string | null;
  className?: string;
}) {
  const t = useTranslation();
  const Icon = SCOPE_ICONS[scope.scopeType] ?? Building2;
  const id = scopeId(scope);
  const organizationWide = scope.scopeType === "Organization";

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded border px-1.5 py-0.5 font-mono text-[11px]",
        organizationWide
          ? "border-attention-200 bg-attention-50 text-attention-700"
          : "border-slate-200 bg-slate-50 text-slate-600",
        className
      )}
      title={
        organizationWide
          ? t("roles.everythingOrganisationHas")
          : `${SCOPE_TYPE_LABELS[scope.scopeType]}${id ? ` — ${id}` : ""}`
      }
    >
      <Icon className="size-3 flex-shrink-0" />
      {organizationWide
        ? "whole organisation"
        : (name ?? (id ? shortId(id) : SCOPE_TYPE_LABELS[scope.scopeType]))}
    </span>
  );
}

export const SCOPE_NOUNS: Record<PermissionScopeType, string> = {
  Organization: "organisation",
  SubOrganization: "business unit",
  RuleSet: "rule set",
  Queue: "queue",
};
