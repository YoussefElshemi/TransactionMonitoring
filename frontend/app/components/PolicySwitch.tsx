"use client";

import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function PolicySwitch({
  label,
  checked,
  inherited = false,
  disabled,
  onChange,
  children,
}: {
  label: string;
  checked: boolean;
  inherited?: boolean;
  disabled?: boolean;
  onChange: (next: boolean) => void;
  children: React.ReactNode;
}) {
  const t = useTranslation();
  return (
    <div className="flex items-start gap-3">
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-label={label}
        disabled={disabled}
        onClick={() => onChange(!checked)}
        className={cn(
          "mt-0.5 inline-flex h-5 w-9 flex-shrink-0 items-center rounded-full transition-colors",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
          "disabled:cursor-not-allowed disabled:opacity-50",
          checked ? "bg-brand-800" : "bg-slate-300"
        )}
      >
        <span
          className={cn(
            "size-4 rounded-full bg-white shadow-sm transition-transform",
            checked ? "translate-x-4" : "translate-x-0.5"
          )}
        />
      </button>

      <div className="space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm font-medium text-slate-900">{label}</span>
          {inherited && (
            <span className="rounded-full bg-slate-100 px-2 py-0.5 font-mono text-[10px] uppercase tracking-widest text-slate-500">
              {t("policySwitch.default")}
            </span>
          )}
        </div>
        <div className="max-w-prose text-xs text-slate-500">{children}</div>
      </div>
    </div>
  );
}
