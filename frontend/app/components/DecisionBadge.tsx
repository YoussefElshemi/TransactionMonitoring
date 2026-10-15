import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { reasonText } from "@/lib/decisions";

const decisionStyles: Record<string, string> = {
  Held: "border-attention-200 bg-attention-50 text-attention-700 ring-1 ring-inset ring-attention-200",
  Released: "border-transparent bg-settled-50 text-settled-700",
  Blocked: "border-critical-solid bg-critical-solid text-critical-solid-foreground",
  Pending: "border-slate-200 bg-slate-100 text-slate-600",
  Expired: "border-slate-200 bg-slate-100 text-slate-600",
};

export function DecisionBadge({
  status,
  reasonCode,
  className,
}: {
  status: string;
  reasonCode?: string;
  className?: string;
}) {
  const classes =
    decisionStyles[status] ?? "border-slate-200 bg-slate-100 text-slate-600";

  return (
    <Badge
      variant="outline"
      className={cn(
        "rounded-md px-2 py-0.5 font-mono text-[11px] font-medium",
        classes,
        className
      )}
      title={reasonCode ? reasonText(reasonCode) : undefined}
    >
      {status}
    </Badge>
  );
}

export function DecisionBadgeWithReason({
  status,
  reasonCode,
  className,
}: {
  status: string;
  reasonCode: string;
  className?: string;
}) {
  return (
    <div className={cn("min-w-0", className)}>
      <DecisionBadge status={status} />
      <p className="mt-1 text-xs text-slate-500">{reasonText(reasonCode)}</p>
    </div>
  );
}
