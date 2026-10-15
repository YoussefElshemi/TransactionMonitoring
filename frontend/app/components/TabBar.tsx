"use client";

import { useRef } from "react";

import { cn } from "@/lib/utils";

interface TabBarProps {
  tabs: string[];
  activeTab: string;
  onChange: (tab: string) => void;
  actions?: React.ReactNode;
  label?: (tab: string) => string;
  "aria-label"?: string;
}

export function TabBar({
  tabs,
  activeTab,
  onChange,
  actions,
  label,
  "aria-label": ariaLabel,
}: TabBarProps) {
  const strip = useRef<HTMLDivElement>(null);

  const onKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    const delta =
      event.key === "ArrowRight" ? 1 : event.key === "ArrowLeft" ? -1 : 0;

    let next: string | undefined;
    if (delta !== 0) {
      const current = tabs.indexOf(activeTab);
      next = tabs[(current + delta + tabs.length) % tabs.length];
    } else if (event.key === "Home") {
      next = tabs[0];
    } else if (event.key === "End") {
      next = tabs[tabs.length - 1];
    }

    if (!next) return;

    event.preventDefault();
    onChange(next);
    strip.current
      ?.querySelector<HTMLButtonElement>(`[data-tab="${CSS.escape(next)}"]`)
      ?.focus();
  };

  return (
    <div className="border-b border-slate-200">
      <div className="flex items-center justify-between gap-3">
        <div
          ref={strip}
          role="tablist"
          aria-label={ariaLabel}
          onKeyDown={onKeyDown}
          className="flex min-w-0 flex-wrap"
        >
          {tabs.map((tab) => {
            const selected = activeTab === tab;

            return (
              <button
                key={tab}
                type="button"
                role="tab"
                data-tab={tab}
                aria-selected={selected}
                // The roving tabindex: exactly one tab is reachable by Tab, and
                // the arrows move within. Without this the strip is six tab
                // stops, which is what makes a long tab bar tedious to get past.
                tabIndex={selected ? 0 : -1}
                onClick={() => onChange(tab)}
                className={cn(
                  "-mb-px flex-shrink-0 whitespace-nowrap border-b-2 px-4 py-2.5 font-mono text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800",
                  selected
                    ? "border-brand-800 text-brand-800"
                    : "border-transparent text-slate-500 hover:text-slate-900"
                )}
              >
                {label ? label(tab) : tab}
              </button>
            );
          })}
        </div>
        {actions && <div className="mb-1 flex-shrink-0">{actions}</div>}
      </div>
    </div>
  );
}
