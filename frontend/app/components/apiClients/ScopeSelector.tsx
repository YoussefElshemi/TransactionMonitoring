"use client";

import { scopeDescription } from "@/lib/apiScopes";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function ScopeSelector({
  options,
  selected,
  onChange,
  disabled = false,
  className
}: {
  options: string[];
  selected: string[];
  onChange: (scopes: string[]) => void;
  disabled?: boolean;
  className?: string;
}) {
  const toggle = (scope: string) => {
    onChange(
      selected.includes(scope)
        ? selected.filter((current) => current !== scope)
        : [...selected, scope]
    );
  };

  return (
    <div className={cn("space-y-2", className)}>
      {options.map((scope) => {
        const description = scopeDescription(scope);

        return (
          <label
            key={scope}
            className={cn(
              "flex cursor-pointer items-start gap-2.5 rounded-lg border border-slate-200 px-3 py-2.5 transition-colors hover:bg-slate-50",
              selected.includes(scope) && "border-brand-200 bg-brand-50",
              disabled && "cursor-not-allowed opacity-60"
            )}
          >
            <input
              type="checkbox"
              checked={selected.includes(scope)}
              onChange={() => toggle(scope)}
              disabled={disabled}
              className="mt-0.5 h-4 w-4 rounded border-slate-300 accent-brand-800"
            />
            <span className="min-w-0">
              <span className="block font-mono text-sm text-slate-900">
                {scope}
              </span>
              {description && (
                <span className="mt-0.5 block text-xs text-slate-500">
                  {description}
                </span>
              )}
            </span>
          </label>
        );
      })}

      {selected.length === 0 && (
        <p className="text-xs text-attention-700">
          A client needs at least one scope — without one it can authenticate
          but cannot call anything.
        </p>
      )}
    </div>
  );
}

export function ScopeBadges({
  scopes,
  max = 2
}: {
  scopes: string[];
  max?: number;
}) {
  const t = useTranslation();
  if (scopes.length === 0) {
    return (
      <span className="font-mono text-xs uppercase tracking-widest text-attention-700">
        {t("apiClients.none")}
      </span>
    );
  }

  const shown = scopes.slice(0, max);
  const hidden = scopes.length - shown.length;

  return (
    <span className="flex flex-wrap items-center gap-1">
      {shown.map((scope) => (
        <span
          key={scope}
          className="rounded-md border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-mono text-[11px] text-slate-600"
        >
          {scope}
        </span>
      ))}
      {hidden > 0 && (
        <span
          className="font-mono text-[11px] text-slate-400"
          title={scopes.join(", ")}
        >
          +{hidden}
        </span>
      )}
    </span>
  );
}
