"use client";

import { FlaskConical } from "lucide-react";

import { useEnvironment } from "@/context/ModeContext";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function EnvironmentSwitcher() {
  const t = useTranslation();
  const { mode, setMode } = useEnvironment();
  const testing = mode === "test";

  return (
    <div className="flex items-center gap-2" aria-label={t("environmentSwitcher.dataEnvironment")}>
      {testing && (
        <span
          role="status"
          className="hidden items-center gap-1 rounded-md border border-attention-300 bg-attention-50 px-2 py-1 font-mono text-[11px] font-medium text-attention-800 sm:inline-flex"
        >
          <FlaskConical aria-hidden="true" className="size-3.5" />
          {t("environmentSwitcher.sandbox")}
        </span>
      )}
      <div
        role="radiogroup"
        aria-label={t("environmentSwitcher.selectLiveOrTestEnvironment")}
        className={cn(
          "inline-flex rounded-md border p-0.5 shadow-sm",
          testing
            ? "border-attention-300 bg-attention-50"
            : "border-slate-200 bg-slate-50"
        )}
      >
        <button
          type="button"
          role="radio"
          aria-checked={!testing}
          onClick={() => setMode("live")}
          className={cn(
            "rounded px-2.5 py-1 font-mono text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600",
            !testing
              ? "bg-card text-slate-900 shadow-sm"
              : "text-attention-800 hover:bg-attention-100"
          )}
        >
          {t("environmentSwitcher.live")}
        </button>
        <button
          type="button"
          role="radio"
          aria-checked={testing}
          onClick={() => setMode("test")}
          className={cn(
            "rounded px-2.5 py-1 font-mono text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600",
            testing
              ? "bg-attention-500 text-white shadow-sm"
              : "text-slate-500 hover:bg-slate-100 hover:text-slate-900"
          )}
        >
          {t("environmentSwitcher.test")}
        </button>
      </div>
    </div>
  );
}
