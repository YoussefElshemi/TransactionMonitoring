"use client";

import { cn } from "@/lib/utils";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export interface SectionGroup {
  labelKey: string;
  sections: string[];
}

interface SectionNavProps {
  groups: SectionGroup[];
  active: string;
  onChange: (section: string) => void;
  className?: string;
}

export function SectionNav({
  groups,
  active,
  onChange,
  className,
}: SectionNavProps) {
  const t = useTranslation();
  return (
    <>
      <div className={cn("lg:hidden", className)}>
        <Select value={active} onValueChange={onChange}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {groups.map((group) => (
              <SelectGroup key={group.labelKey}>
                <SelectLabel>{t(group.labelKey)}</SelectLabel>
                {group.sections.map((section) => (
                  <SelectItem key={section} value={section}>
                    {section}
                  </SelectItem>
                ))}
              </SelectGroup>
            ))}
          </SelectContent>
        </Select>
      </div>

      <nav
        aria-label={t("sectionNav.sections")}
        className={cn(
          "hidden lg:sticky lg:top-0 lg:block lg:max-h-[calc(100vh-8rem)] lg:overflow-y-auto lg:border-r lg:border-slate-200 lg:pr-3",
          className
        )}
      >
        {groups.map((group) => (
          <div key={group.labelKey} className="pb-3 last:pb-0">
            <div className="px-3 pb-1 pt-3 font-mono text-[11px] uppercase tracking-wider text-slate-400">
              {t(group.labelKey)}
            </div>
            {group.sections.map((section) => (
              <button
                key={section}
                onClick={() => onChange(section)}
                aria-current={active === section ? "true" : undefined}
                className={cn(
                  "flex w-full items-center border-l-2 px-3 py-1.5 text-left font-mono text-[13px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-800",
                  active === section
                    ? "border-brand-800 bg-brand-50 font-medium text-brand-800"
                    : "border-transparent text-slate-500 hover:bg-slate-100 hover:text-slate-900"
                )}
              >
                {section}
              </button>
            ))}
          </div>
        ))}
      </nav>
    </>
  );
}
