"use client";

import { Users } from "lucide-react";

import { SubOrganizationUsage } from "@/types";
import { shortId } from "@/lib/resources";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function UsageWarning({
  usage,
  unavailableNote,
  className,
}: {
  usage: SubOrganizationUsage | null;
  unavailableNote: string;
  className?: string;
}) {
  const t = useTranslation();

  if (!usage) {
    return (
      <p className={cn("text-sm text-slate-500", className)}>
        {unavailableNote}
      </p>
    );
  }

  const rows: { label: string; count: number; note?: string }[] = [
    {
      label: t(
        usage.userCount === 1
          ? "subOrganizations.personHomedHere"
          : "subOrganizations.peopleHomedHere"
      ),
      count: usage.userCount,
      note: t("subOrganizations.permissionsStopBeingNarrowed"),
    },
    {
      label: t(
        usage.queueCount === 1 ? "subOrganizations.queue" : "subOrganizations.queues"
      ),
      count: usage.queueCount,
    },
    {
      label: t(
        usage.ruleSetCount === 1
          ? "subOrganizations.ruleSet"
          : "subOrganizations.ruleSets"
      ),
      count: usage.ruleSetCount,
    },
    {
      label:
        usage.roleAssignmentCount === 1
          ? "grant scoped to it"
          : "grants scoped to it",
      count: usage.roleAssignmentCount,
      note: "those grants no longer name anything",
    },
  ];

  const empty = rows.every((row) => row.count === 0);

  if (empty) {
    return (
      <p className={cn("text-sm text-slate-600", className)}>
        Nothing is in {usage.name} — no people, queues, rule sets or grants — so
        removing it changes nobody&apos;s access.
      </p>
    );
  }

  return (
    <div
      className={cn(
        "space-y-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2.5 ring-1 ring-inset ring-attention-200",
        className
      )}
    >
      <p className="flex items-center gap-2 text-sm font-medium text-attention-700">
        <Users className="size-4 flex-shrink-0" />
        {usage.name} is not empty
      </p>
      <ul className="space-y-1 text-sm text-slate-600">
        {rows
          .filter((row) => row.count > 0)
          .map((row) => (
            <li key={row.label}>
              <span className="font-mono">{row.count}</span> {row.label}
              {row.note && (
                <span className="block text-xs text-slate-500">{row.note}</span>
              )}
            </li>
          ))}
      </ul>
    </div>
  );
}

export function HomeUnitCell({
  subOrganizationId,
  name,
  className,
}: {
  subOrganizationId: string | null;
  name?: string | null;
  className?: string;
}) {
  const t = useTranslation();
  if (!subOrganizationId) {
    return (
      <span
        className={cn("font-mono text-xs text-slate-400", className)}
        title={t("subOrganizations.notInABusinessUnit")}
      >
        whole organisation
      </span>
    );
  }

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-mono text-[11px] text-slate-600",
        className
      )}
      title={`Homed in one business unit (${subOrganizationId}). Every organisation-wide permission this account holds is narrowed to it.`}
    >
      <Users className="size-3 flex-shrink-0" />
      {name ?? shortId(subOrganizationId)}
    </span>
  );
}
