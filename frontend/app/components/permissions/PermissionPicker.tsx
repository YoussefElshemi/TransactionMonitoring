"use client";

import { useMemo, useState } from "react";
import { ChevronDown, ChevronRight, Lock, Search } from "lucide-react";

import { PermissionCatalogueEntry } from "@/types";
import { actionLabel } from "@/lib/permissions";
import { groupByResource, usePermissionCatalogue } from "@/lib/roles";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function PermissionPicker({
  selected,
  onChange,
  disabled = false,
  readOnly = false,
}: {
  selected: string[];
  onChange: (permissions: string[]) => void;
  disabled?: boolean;
  readOnly?: boolean;
}) {
  const t = useTranslation();
  const { data: catalogue, loading, unavailable } = usePermissionCatalogue(true);
  const [search, setSearch] = useState("");
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const chosen = useMemo(() => new Set(selected), [selected]);

  const groups = useMemo(() => {
    const term = search.trim().toLowerCase();
    const matching = term
      ? catalogue.filter(
          (entry) =>
            entry.name.toLowerCase().includes(term) ||
            entry.resource?.toLowerCase().includes(term)
        )
      : catalogue;
    return groupByResource(matching);
  }, [catalogue, search]);

  const toggle = (entry: PermissionCatalogueEntry) => {
    if (disabled || readOnly || !entry.isGrantable || !entry.isHeld) return;
    onChange(
      chosen.has(entry.name)
        ? selected.filter((name) => name !== entry.name)
        : [...selected, entry.name]
    );
  };

  const selectableIn = (entries: PermissionCatalogueEntry[]) =>
    entries.filter((entry) => entry.isGrantable && entry.isHeld);

  const toggleGroup = (entries: PermissionCatalogueEntry[]) => {
    const selectable = selectableIn(entries);
    if (!selectable.length) return;
    const allChosen = selectable.every((entry) => chosen.has(entry.name));
    const names = new Set(selectable.map((entry) => entry.name));
    onChange(
      allChosen
        ? selected.filter((name) => !names.has(name))
        : [...selected, ...selectable
            .filter((entry) => !chosen.has(entry.name))
            .map((entry) => entry.name)]
    );
  };

  if (loading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 6 }).map((_, index) => (
          <Skeleton key={index} className="h-9 w-full" />
        ))}
      </div>
    );
  }

  if (unavailable) {
    return (
      <div className="rounded-md border border-dashed border-slate-200 bg-card px-6 py-10 text-center">
        <p className="text-sm text-slate-500">
          {t("permissions.thePermissionCatalogueCouldNot")}
        </p>
        <p className="mx-auto mt-2 max-w-md text-xs text-slate-400">
          {t("permissions.composingNeedsBoth", "permissions:read", "roles:write")}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={t("permissions.filterByResourceOrAction")}
            className="pl-8"
          />
        </div>
        <p className="flex-shrink-0 font-mono text-xs text-slate-500">
          {selected.length} selected
        </p>
      </div>

      {groups.length === 0 ? (
        <p className="rounded-md border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-500">
          {t("permissions.nothingMatchesThat")}
        </p>
      ) : (
        <div className="max-h-[28rem] space-y-1 overflow-y-auto rounded-md border border-slate-200 p-2">
          {groups.map((group) => {
            const open = search.trim() !== "" || !collapsed.has(group.resource);
            const selectable = selectableIn(group.entries);
            const chosenHere = group.entries.filter((entry) =>
              chosen.has(entry.name)
            ).length;

            return (
              <div key={group.resource}>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() =>
                      setCollapsed((current) => {
                        const next = new Set(current);
                        if (next.has(group.resource)) next.delete(group.resource);
                        else next.add(group.resource);
                        return next;
                      })
                    }
                    className="flex flex-1 items-center gap-2 rounded px-2 py-1.5 text-left hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800"
                  >
                    {open ? (
                      <ChevronDown className="size-3.5 text-slate-400" />
                    ) : (
                      <ChevronRight className="size-3.5 text-slate-400" />
                    )}
                    <span className="font-mono text-xs font-medium uppercase tracking-wider text-slate-600">
                      {group.label}
                    </span>
                    {chosenHere > 0 && (
                      <span className="font-mono text-[10px] text-brand-800">
                        {chosenHere}/{group.entries.length}
                      </span>
                    )}
                  </button>
                  {!readOnly && selectable.length > 0 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="h-7 px-2 font-mono text-[11px]"
                      disabled={disabled}
                      onClick={() => toggleGroup(group.entries)}
                    >
                      {selectable.every((entry) => chosen.has(entry.name))
                        ? "None"
                        : "All"}
                    </Button>
                  )}
                </div>

                {open && (
                  <div className="ml-6 space-y-0.5 pb-1">
                    {group.entries.map((entry) => (
                      <PermissionRow
                        key={entry.name}
                        entry={entry}
                        checked={chosen.has(entry.name)}
                        disabled={disabled}
                        readOnly={readOnly}
                        onToggle={() => toggle(entry)}
                      />
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

function PermissionRow({
  entry,
  checked,
  disabled,
  readOnly,
  onToggle,
}: {
  entry: PermissionCatalogueEntry;
  checked: boolean;
  disabled: boolean;
  readOnly: boolean;
  onToggle: () => void;
}) {
  const blocked = !entry.isGrantable
    ? "No role may carry this. Holding it means being in the built-in Admin role, which is the only way to act on another organisation."
    : !entry.isHeld
      ? "You don't hold this yourself, and a role can only carry permissions its composer already has."
      : null;

  return (
    <label
      className={cn(
        "flex items-center gap-2.5 rounded px-2 py-1 text-sm",
        blocked || readOnly
          ? "cursor-default"
          : "cursor-pointer hover:bg-slate-50"
      )}
      title={blocked ?? undefined}
    >
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled || readOnly || Boolean(blocked)}
        onChange={onToggle}
        className="h-3.5 w-3.5 rounded border-slate-300 accent-brand-800 disabled:opacity-50"
      />
      <span
        className={cn(
          "font-mono text-xs",
          blocked ? "text-slate-400" : "text-slate-700"
        )}
      >
        {actionLabel(entry.action || entry.name)}
      </span>
      {blocked && <Lock className="size-3 flex-shrink-0 text-slate-300" />}
      {!entry.isGrantable && (
        <span className="font-mono text-[10px] uppercase tracking-wider text-slate-400">
          never grantable
        </span>
      )}
      {entry.isGrantable && !entry.isHeld && (
        <span className="font-mono text-[10px] uppercase tracking-wider text-slate-400">
          not yours to give
        </span>
      )}
    </label>
  );
}
