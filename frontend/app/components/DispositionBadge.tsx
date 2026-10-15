"use client";

import {
  CircleCheck,
  CircleMinus,
  CircleSlash,
  TriangleAlert,
} from "lucide-react";

import { CaseDisposition } from "@/types";
import { cn } from "@/lib/utils";
import { dispositionLabelKey } from "@/lib/cases";

import { useTranslation } from "@/context/LocalizationContext";

const DISPOSITION_STYLES: Record<
  CaseDisposition,
  { className: string; icon: typeof CircleCheck; titleKey: string }
> = {
  TruePositive: {
    className: "border-dashed border-elevated-200 text-elevated-700",
    icon: CircleCheck,
    titleKey: "disposition.badge.truePositive",
  },
  FalsePositive: {
    className: "border-dashed border-slate-300 text-slate-500",
    icon: CircleSlash,
    titleKey: "disposition.badge.falsePositive",
  },
  TrueNegative: {
    className: "border-dashed border-slate-300 text-slate-500",
    icon: CircleMinus,
    titleKey: "disposition.badge.trueNegative",
  },
  FalseNegative: {
    className: "border-critical-500 bg-critical-50 text-critical-700",
    icon: TriangleAlert,
    titleKey: "disposition.badge.falseNegative",
  },
};

const UNKNOWN = {
  className: "border-dashed border-slate-300 text-slate-500",
  icon: CircleSlash,
  titleKey: "",
};

export function DispositionBadge({
  disposition,
  className,
}: {
  disposition: string;
  className?: string;
}) {
  const t = useTranslation();

  const {
    className: styles,
    icon: Icon,
    titleKey,
  } = DISPOSITION_STYLES[disposition as CaseDisposition] ?? UNKNOWN;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[11px] font-medium",
        styles,
        className
      )}
      title={titleKey ? t(titleKey) : undefined}
    >
      <Icon className="size-3" />
      {t(dispositionLabelKey(disposition))}
    </span>
  );
}
