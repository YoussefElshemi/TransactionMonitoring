import { RuleSeverity } from "@/types";
import { useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";

const severityStyles: Record<RuleSeverity, string> = {
  // `slate-600` rather than `slate-500`: 4.34:1 on `slate-100` in light mode,
  // which is the same failure in the neutral rung.
  Info: "bg-slate-100 text-slate-600",
  Low: "bg-settled-50 text-settled-700",
  Medium: "bg-attention-50 text-attention-700",
  High: "bg-elevated-50 text-elevated-700",
  Critical: "bg-critical-50 text-critical-700",
};

const strongSeverityStyles: Record<RuleSeverity, string> = {
  Info: "border-slate-200 bg-slate-50 text-slate-600",
  Low: "border-settled-200 bg-settled-50 text-settled-700",
  Medium: "border-attention-500 bg-attention-50 text-attention-700",
  High: "border-elevated-500 bg-elevated-50 text-elevated-700 ring-1 ring-inset ring-elevated-500",
  Critical: "border-critical-solid bg-critical-solid text-critical-solid-foreground",
};

export function SeverityBadge({
  severity,
  emphasis = false,
  className,
}: {
  severity: RuleSeverity | (string & {});
  emphasis?: boolean;
  className?: string;
}) {
  const t = useTranslation();
  const styles = emphasis ? strongSeverityStyles : severityStyles;

  const label = t(`severity.${String(severity).toLowerCase()}`);
  const text = label.startsWith("severity.") ? severity : label;

  return (
    <span
      className={cn(
        "inline-flex items-center rounded px-1.5 py-0.5 font-mono text-[11px] font-medium",
        emphasis && "border",
        styles[severity as RuleSeverity] ?? styles.Info,
        className
      )}
    >
      {text}
    </span>
  );
}
