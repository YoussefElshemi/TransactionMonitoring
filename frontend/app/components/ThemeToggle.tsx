"use client";

import { Monitor, Moon, Sun } from "lucide-react";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ThemePreference, useTheme } from "@/context/ThemeContext";
import { useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";

const OPTIONS: { value: ThemePreference; labelKey: string; Icon: typeof Sun }[] = [
  { value: "light", labelKey: "header.theme.light", Icon: Sun },
  { value: "dark", labelKey: "header.theme.dark", Icon: Moon },
  { value: "system", labelKey: "header.theme.system", Icon: Monitor },
];

export function ThemeToggle({ className }: { className?: string }) {
  const t = useTranslation();
  const { preference, theme, setPreference } = useTheme();
  const TriggerIcon = theme === "dark" ? Moon : Sun;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label={`${t("header.theme.toggle")} — ${t(
            `header.theme.${preference}`
          )}`}
          aria-haspopup="menu"
          className={cn(
            "inline-flex size-8 cursor-pointer items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
            className
          )}
        >
          <TriggerIcon aria-hidden className="size-4" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-36">
        {OPTIONS.map(({ value, labelKey, Icon }) => (
          <DropdownMenuItem
            key={value}
            onClick={() => setPreference(value)}
            className={cn(
              "gap-2 font-mono text-xs",
              preference === value && "text-brand-800"
            )}
          >
            <Icon aria-hidden className="size-3.5" />
            {t(labelKey)}
            {preference === value && (
              <span className="sr-only">{t("common.selected")}</span>
            )}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
